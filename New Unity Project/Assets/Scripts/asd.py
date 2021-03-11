from operator import itemgetter

from pyramid.decorator import reify

from ems.constants import VOTE_CHECK_AMOUNTS  # Min VP Votes (Privacy Act)
from ems.models import Setting
from ems.e9.e9_report import E9Report
from ems.e9.util import safe_divide
from ems.models import Electorate, VotesType, CinfoType
from ems.template import e9_percentformat


class VotesByVotingPlaceEachElectorate(E9Report):
    template_filename = 'votes_by_voting_place_each_electorate.html.jinja2'
    formats = ['html', 'csv']

    def __init__(self, context, base_data):
        super().__init__(context, base_data)
        self.min_vp_votes = Setting.get_value(context.ems_db, VOTE_CHECK_AMOUNTS)

    @reify
    def all_valid_model_parameters(self):
        return (e.number for e in self.ems_db.query(Electorate))

    def _property_name_street(self, property_name, street):
        property_name_street = []
        if property_name:
            property_name_street.append(property_name)
        if street:
            property_name_street.append(street)

        return ', '.join(property_name_street)

    def _initialize_total_row(self, row, total_row):
        for c in range(3, len(row)):
            total_row.append(0)

        return total_row

    def _track_totals(self, row, total_row):
        # If list hasn't been fully initialized
        if len(total_row) <= 3:
            total_row = self._initialize_total_row(row, total_row)
        for c in range(3, len(row)):
            if row[c]:
                total_row[c] += row[c]

    def write_csv(self, csv, model):
        # This will be per electorate at least, use hardcoded electorate for time being
        # file name will use electorate.number not id
        csv.writerow(['Party and Electorate Candidate Votes Recorded at each Voting Place'])

        header_row = []
        for idx, heading in enumerate(model['headings']):
            header_row.append(heading)
            # Write first two column of header on their own row
            if idx == 1:
                csv.writerow(header_row)
                header_row = ['', '']
        csv.writerow(header_row)

        for section in [
            [model['advance_vps'], 'normal', 'Advance Voting Places'],
            [model['ed_vps'], 'normal', 'Voting Places'],
            [model['special_vps'], 'specials', ''],
            [[model['vp_totals']], 'total', '']
        ]:
            row_data = section[0]
            row_type = section[1]
            row_header = section[2]

            prev_general_electorate = None
            for idx, row in enumerate(row_data):
                if idx == 0 and row_header:
                    csv.writerow([row_header])

                if row_type == 'total':
                    csv.writerow(row[1:])
                else:
                    if row[0] and row[0] != prev_general_electorate and row_type != 'specials':
                        csv.writerow([row[0]])
                    prev_general_electorate = row[0]

                    csv.writerow(row[1:])

            csv.writerow([])

        csv.writerow(['', model['grand_total']['label'], '', model['grand_total']['vote_total']])
        csv.writerow([])

        for idx, summ in enumerate(model['summary']):
            if self.report_type == 'party':
                summ = summ[1:]

            if idx > 0:
                csv.writerow(summ[:-1])
            else:
                csv.writerow(summ)


class CandidateVotesByVotingPlaceEachElectorate(VotesByVotingPlaceEachElectorate):
    report_type = 'candidate'

    def get_model(self, electorate_number):
        # Note: general_electorate is physical electorate but only for maori electorates
        #  this allows the template to break up voting place into sections for maori electorates
        result = self.context.ems_db.execute("""
select e.number as electorate_number, e.name as electorate_name, e.is_maori,
   case when e.is_maori then pe.number else e.number end as electorate_order,
   case when e.is_maori then pe.name else e.name end as general_electorate,
   p.votes_type <> :standard_votes_type as for_official_count_only,
   vp.voting_place_id,
   vp.is_advance_vp,
   cinfo.address3 as suburb,
   cinfo.address1 as property_name,
   cinfo.address2 as street,
   case when pn.first_name is not null then concat(pn.last_name, ', ', pn.first_name)
   else pn.last_name end as candidate_fullname,
   party.name as party_name,
   sum(cv.vote_total) as vote_total,
   sum(coalesce(ov.vote_total, 0)) as informal_vote_total
  from electorate e
  join evp on (evp.electorate_id = e.electorate_id)
  join electorate pe on (pe.electorate_id = evp.physical_electorate_id)
  join voting_place vp on (vp.voting_place_id = evp.voting_place_id)
  join property p on (p.property_id = vp.property_id)
  join property_cinfo pcinfo on (pcinfo.property_id = p.property_id)
  join contactinfo cinfo on (
    pcinfo.contactinfo_id = cinfo.contactinfo_id and cinfo.cinfo_type = :cinfo_type
  )
  join evp_phase phase on (phase.evp_id = evp.evp_id)
  join resultset rs on (rs.evp_phase_id = phase.evp_phase_id)
  join election_mode m on (m.election_mode_id = rs.election_mode_id)
  join candidate_vote cv on (cv.resultset_id = rs.resultset_id)
  join other_vote ov on (ov.resultset_id = rs.resultset_id)
  join other_vote_total ovt on (
   ovt.other_vote_total_id = ov.other_vote_total_id and ovt.abbreviation = :ovt_abbreviation
  )
  join electorate_candidate ec on (ec.electorate_candidate_id = cv.electorate_candidate_id)
  join candidate c on (c.candidate_id = ec.candidate_id)
  join person pn on (c.person_id = pn.person_id)
  left outer join party_candidate pc on (pc.candidate_id = c.candidate_id)
  left outer join party on (party.party_id = pc.party_id)
 where e.number = :electorate_number
   and m.election_mode_id = (select max(resultset.election_mode_id) AS max FROM resultset)
   and vp.is_active
   and p.votes_type NOT IN (:party_only_votes_type, :overseas_party_only_votes_type)
   and rs.result_rcvd_time is not null
 group by e.is_maori, e.number, e.name, p.votes_type, vp.voting_place_id,
   vp.is_advance_vp, suburb, property_name, street,
   pn.first_name, pn.last_name, candidate_fullname, party_name, pe.number, pe.name
 order by e.is_maori, electorate_order, general_electorate,
   for_official_count_only, vp.is_advance_vp,
   cinfo.address3, cinfo.address1, cinfo.address2,
   vp.voting_place_id,
   normalize_name(pn.first_name, pn.last_name), general_electorate""", {
            'standard_votes_type': VotesType.STANDARD.value,
            'electorate_number': int(electorate_number),
            'party_only_votes_type': VotesType.PARTY_ONLY.value,
            'overseas_party_only_votes_type': VotesType.OVERSEAS_PARTY_ONLY.value,
            'cinfo_type': CinfoType.LOCATION.value,
            'ovt_abbreviation': 'CINF'
        }
        )

        headings = []
        advance_vps = []
        ed_vps = []
        special_vps = []
        vp_totals = ['', '']
        less_than_6_votes_total = [
            '', '', 'Voting places where less than ' + str(int(self.min_vp_votes)) + ' votes were taken'
        ]

        # We get one row per candidate so need to combine into one row per voting place and some tracking variables
        headings_populated = False
        column_name_mapping = {}
        candidate_party_mapping = {}
        results_per_voting_place = []
        vp_vote_total = 0
        previous_row = None
        electorate_number = None
        for idx, row in enumerate(result):
            row = dict(zip(row.keys(), row))

            if not previous_row:
                general_electorate = row['general_electorate'] if row['is_maori'] else ''
                property_name_street = self._property_name_street(row['property_name'], row['street'])
                results_per_voting_place = [
                    general_electorate, row['suburb'], property_name_street, row['vote_total']
                ]
                headings.extend([
                    '{} {}'.format(row['electorate_name'], row['electorate_number']),
                    'Candidate Vote Details'
                ])
                headings.append(row['candidate_fullname'])
                column_name_mapping[idx] = row['candidate_fullname']
                candidate_party_mapping[idx] = row['party_name'] if row['party_name'] else 'Independent'
                vp_totals.append('{} Total'.format(row['electorate_name']))
                electorate_number = row['electorate_number']
                electorate_name = row['electorate_name']
            elif previous_row and row['voting_place_id'] != previous_row['voting_place_id']:
                if not headings_populated:
                    headings.extend(['Total Valid Candidate Votes', 'Informal Candidate Votes'])
                    headings_populated = True
                results_per_voting_place.extend([
                    vp_vote_total,
                    previous_row['informal_vote_total']
                ])
                vp_vote_total = 0
                # Assign complete row to relevant list section
                if previous_row['for_official_count_only']:
                    special_vps.append(results_per_voting_place)
                elif (results_per_voting_place[-2] + results_per_voting_place[-1]) < self.min_vp_votes:
                    # Track voting places total with less than minimum votes
                    self._track_totals(results_per_voting_place, less_than_6_votes_total)
                elif previous_row['is_advance_vp']:
                    advance_vps.append(results_per_voting_place)
                else:
                    ed_vps.append(results_per_voting_place)

                # Only one total across all voting places
                self._track_totals(results_per_voting_place, vp_totals)

                # Only output suburb if different from previous suburb, makes template and csv easier
                general_electorate = row['general_electorate'] if row['is_maori'] else ''
                if row['suburb'] != previous_row['suburb'] \
                        or row['is_advance_vp'] != previous_row['is_advance_vp']:
                    results_per_voting_place = [general_electorate, row['suburb']]
                else:
                    results_per_voting_place = [general_electorate, '']

                property_name_street = self._property_name_street(row['property_name'], row['street'])
                results_per_voting_place.extend([property_name_street, row['vote_total']])
            else:
                if not headings_populated:
                    headings.append(row['candidate_fullname'])
                    column_name_mapping[idx] = row['candidate_fullname']
                    candidate_party_mapping[idx] = row['party_name'] if row['party_name'] else 'Independent'

                results_per_voting_place.append(row['vote_total'])

            vp_vote_total += row['vote_total']

            previous_row = row.copy()

        # Add last row and track total
        results_per_voting_place.extend([
            vp_vote_total,
            previous_row['informal_vote_total']
        ])

        # Assign complete row to relevant list section
        if previous_row['for_official_count_only']:
            special_vps.append(results_per_voting_place)
        elif (results_per_voting_place[-2] + results_per_voting_place[-1]) < self.min_vp_votes:
            # Track voting places total with less than minimum votes
            self._track_totals(results_per_voting_place, less_than_6_votes_total)
        elif previous_row['is_advance_vp']:
            advance_vps.append(results_per_voting_place)
        else:
            ed_vps.append(results_per_voting_place)

        # Only one total across all voting places
        self._track_totals(results_per_voting_place, vp_totals)

        # Pop voting place total with less than six votes in special votes section
        special_vps.append(less_than_6_votes_total)

        grand_total, winning_candidate, summary = self._calc_grand_total_winning_cand_and_summary(
            vp_totals, column_name_mapping, candidate_party_mapping
        )

        return {
            'report_type': self.report_type,
            'electorate_number': electorate_number,
            'electorate_name': electorate_name,
            'headings': headings,
            'advance_vps': advance_vps,
            'ed_vps': ed_vps,
            'special_vps': special_vps,
            'vp_totals': vp_totals,
            'grand_total': grand_total,
            'winning_candidate': winning_candidate,
            'summary': summary
        }

    def _calc_grand_total_winning_cand_and_summary(
        self, vp_totals, column_name_mapping, candidate_party_mapping
    ):

        grand_total = {
            'label': 'Valid Candidate Votes plus Informal Candidate Votes',
            'vote_total': int(vp_totals[-1:][0]) + int(vp_totals[-2:-1][0])
        }

        # Calculate winning candidate and margin
        total_candidate_mapping = {}
        for idx, cand_total in enumerate(vp_totals[3:-2]):
            total_candidate_mapping[idx] = cand_total

        candidates_by_total = sorted(total_candidate_mapping.items(), key=itemgetter(1), reverse=True)

        # If there is only one candidate they win
        if len(total_candidate_mapping) == 1:
            winning_candidate = {
                'name': column_name_mapping[candidates_by_total[0][0]],
                'margin': candidates_by_total[0][1]
            }
        else:
            winning_candidate = {
                'name': column_name_mapping[candidates_by_total[0][0]],
                'margin': candidates_by_total[0][1] - candidates_by_total[1][1]
            }

        summary = [
            [
                'Electorate Candidate Valid Votes',
                'Party',
                '', '',
                '{} - majority {}'.format(winning_candidate['name'], winning_candidate['margin'])
            ]
        ]

        for idx, cand_total in enumerate(vp_totals[3:-2]):
            summary.append([
                column_name_mapping[idx],
                candidate_party_mapping[idx],
                total_candidate_mapping[idx],
                e9_percentformat(
                    safe_divide(total_candidate_mapping[idx], int(vp_totals[-2:-1][0])) * 100.0
                ),
                int(safe_divide(total_candidate_mapping[idx], int(vp_totals[-2:-1][0])) * 100)
            ])

        return grand_total, winning_candidate, summary


class PartyVotesByVotingPlaceEachElectorate(VotesByVotingPlaceEachElectorate):
    report_type = 'party'

    def get_model(self, electorate_number):
        # Note: general_electorate is physical electorate but only for maori electorates
        #  this allows the template to break up voting place into sections for maori electorates
        result = self.context.ems_db.execute("""
select e.number as electorate_number, e.name as electorate_name, e.is_maori,
   case when e.is_maori then pe.number else e.number end as electorate_order,
   case when e.is_maori then pe.name else e.name end as general_electorate,
   p.votes_type <> :standard_votes_type as for_official_count_only,
   vp.voting_place_id,
   vp.is_advance_vp,
   cinfo.address3 as suburb,
   cinfo.address1 as property_name,
   cinfo.address2 as street,
   party.name as party_name,
   sum(pv.vote_total) as vote_total,
   sum(coalesce(ov.vote_total, 0)) as informal_vote_total
  from electorate e
  join evp on (evp.electorate_id = e.electorate_id)
  join electorate pe on (pe.electorate_id = evp.physical_electorate_id)
  join voting_place vp on (vp.voting_place_id = evp.voting_place_id)
  join property p on (p.property_id = vp.property_id)
  join property_cinfo pcinfo on (pcinfo.property_id = p.property_id)
  join contactinfo cinfo on (
    pcinfo.contactinfo_id = cinfo.contactinfo_id and cinfo.cinfo_type = :cinfo_type
  )
  join evp_phase phase on (phase.evp_id = evp.evp_id)
  join resultset rs on (rs.evp_phase_id = phase.evp_phase_id)
  join election_mode m on (m.election_mode_id = rs.election_mode_id)
  join party_vote pv on (pv.resultset_id = rs.resultset_id)
  join other_vote ov on (ov.resultset_id = rs.resultset_id)
  join other_vote_total ovt on (
   ovt.other_vote_total_id = ov.other_vote_total_id and ovt.abbreviation = :ovt_abbreviation
  )
  join party_list pl on (pl.party_list_id = pv.party_list_id)
  join party on (party.party_id = pl.party_id)
 where e.number = :electorate_number
   and m.election_mode_id = (select max(resultset.election_mode_id) AS max FROM resultset)
   and vp.is_active
 group by e.is_maori, e.number, e.name, p.votes_type, vp.voting_place_id,
   vp.is_advance_vp, suburb, property_name, street,
   party_name, pe.number, pe.name
 order by e.is_maori, electorate_order, general_electorate,
   for_official_count_only, vp.is_advance_vp,
   cinfo.address3, cinfo.address1, cinfo.address2,
   vp.voting_place_id,
   party.name, general_electorate""", {
            'standard_votes_type': VotesType.STANDARD.value,
            'electorate_number': int(electorate_number),
            'cinfo_type': CinfoType.LOCATION.value,
            'ovt_abbreviation': 'PINF'
        }
        )

        headings = []
        advance_vps = []
        ed_vps = []
        special_vps = []
        vp_totals = ['', '']
        less_than_6_votes_total = [
            '', '', 'Voting places where less than ' + str(int(self.min_vp_votes)) + ' votes were taken'
        ]

        # We get one row per party so need to combine into one row per voting place
        headings_populated = False
        column_name_mapping = {}
        results_per_voting_place = []
        vp_vote_total = 0
        previous_row = None
        electorate_number = None
        for idx, row in enumerate(result):
            row = dict(zip(row.keys(), row))

            if not previous_row:
                general_electorate = row['general_electorate'] if row['is_maori'] else ''
                property_name_street = self._property_name_street(row['property_name'], row['street'])
                results_per_voting_place = [
                    general_electorate, row['suburb'], property_name_street, row['vote_total']
                ]
                headings.extend([
                    '{} {}'.format(row['electorate_name'], row['electorate_number']),
                    'Party Vote Details'
                ])
                headings.append(row['party_name'])
                column_name_mapping[idx] = row['party_name']
                vp_totals.append('{} Total'.format(row['electorate_name']))
                electorate_number = row['electorate_number']
                electorate_name = row['electorate_name']
            elif previous_row and row['voting_place_id'] != previous_row['voting_place_id']:
                if not headings_populated:
                    headings.extend(['Total Valid Party Votes', 'Informal Party Votes'])
                    headings_populated = True
                results_per_voting_place.extend([
                    vp_vote_total,
                    previous_row['informal_vote_total']
                ])
                vp_vote_total = 0
                # Assign complete row to relevant list section
                if previous_row['for_official_count_only']:
                    special_vps.append(results_per_voting_place)
                elif (results_per_voting_place[-2] + results_per_voting_place[-1]) < self.min_vp_votes:
                    # Track voting places total with less than minimum votes
                    self._track_totals(results_per_voting_place, less_than_6_votes_total)
                elif previous_row['is_advance_vp']:
                    advance_vps.append(results_per_voting_place)
                else:
                    ed_vps.append(results_per_voting_place)

                # Only one total across all voting places
                self._track_totals(results_per_voting_place, vp_totals)

                # Only output suburb if different from previous suburb, makes template and csv easier
                general_electorate = row['general_electorate'] if row['is_maori'] else ''
                if row['suburb'] != previous_row['suburb'] \
                        or row['is_advance_vp'] != previous_row['is_advance_vp']:
                    results_per_voting_place = [general_electorate, row['suburb']]
                else:
                    results_per_voting_place = [general_electorate, '']

                property_name_street = self._property_name_street(row['property_name'], row['street'])
                results_per_voting_place.extend([property_name_street, row['vote_total']])
            else:
                if not headings_populated:
                    headings.append(row['party_name'])
                    column_name_mapping[idx] = row['party_name']

                results_per_voting_place.append(row['vote_total'])

            vp_vote_total += row['vote_total']

            previous_row = row.copy()

        # Add last row and track total
        results_per_voting_place.extend([
            vp_vote_total,
            previous_row['informal_vote_total']
        ])

        # Assign complete row to relevant list section
        if previous_row['for_official_count_only']:
            special_vps.append(results_per_voting_place)
        elif (results_per_voting_place[-2] + results_per_voting_place[-1]) < self.min_vp_votes:
            # Track voting places total with less than minimum votes
            self._track_totals(results_per_voting_place, less_than_6_votes_total)
        elif previous_row['is_advance_vp']:
            advance_vps.append(results_per_voting_place)
        else:
            ed_vps.append(results_per_voting_place)

        # Only one total across all voting places
        self._track_totals(results_per_voting_place, vp_totals)

        # Pop voting place total with less than six votes in special votes section
        special_vps.append(less_than_6_votes_total)

        grand_total, winning_party, summary = self._calc_grand_total_winning_party_and_summary(
            vp_totals, column_name_mapping
        )

        return {
            'report_type': self.report_type,
            'electorate_number': electorate_number,
            'electorate_name': electorate_name,
            'headings': headings,
            'advance_vps': advance_vps,
            'ed_vps': ed_vps,
            'special_vps': special_vps,
            'vp_totals': vp_totals,
            'grand_total': grand_total,
            'winning_party': winning_party,
            'summary': summary
        }

    def _calc_grand_total_winning_party_and_summary(
        self, vp_totals, column_name_mapping
    ):

        grand_total = {
            'label': 'Valid Party Votes plus Informal Party Votes',
            'vote_total': int(vp_totals[-1:][0]) + int(vp_totals[-2:-1][0])
        }

        # Calculate winning party
        total_party_mapping = {}
        for idx, total in enumerate(vp_totals[3:-2]):
            total_party_mapping[idx] = total

        parties_by_total = sorted(total_party_mapping.items(), key=itemgetter(1), reverse=True)
        winning_party = {
            'name': column_name_mapping[parties_by_total[0][0]],
            'percent': e9_percentformat(safe_divide(parties_by_total[0][1], vp_totals[-2:-1][0]) * 100.0)
        }

        summary = [
            [
                '',  # Template will ignore first column for party report type
                'Electorate Party Valid Votes',
                '', '',
                '{} - {}%'.format(winning_party['name'], winning_party['percent'])
            ]
        ]

        for idx, cand_total in enumerate(vp_totals[3:-2]):
            summary.append([
                '',  # Template will ignore first column for party report type
                column_name_mapping[idx],
                total_party_mapping[idx],
                e9_percentformat(safe_divide(total_party_mapping[idx], int(vp_totals[-2:-1][0])) * 100.0),
                safe_divide(total_party_mapping[idx], int(vp_totals[-2:-1][0])) * 100.0
            ])

        return grand_total, winning_party, summary
