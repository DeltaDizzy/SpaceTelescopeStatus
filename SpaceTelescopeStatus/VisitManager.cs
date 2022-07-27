using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceTelescopeStatus
{
    public class VisitManager
    {
        private List<JWSTVisit> visits = new List<JWSTVisit>();


        public void AddVisitByIndex(string visitInfo)
        {
            //JWSTVisit visit = new JWSTVisit(line[0..13], line[15..25], line[27..56], line[58..78], line[80..91], line[93..143], line[145..176], line[178..208], line[210..242]);
            JWSTVisit visit = new JWSTVisit();

            // parse strings
            visit.VisitID = TryParseColumn(0, 13, visitInfo);
            visit.PCSMode = TryParseColumn(15, 25, visitInfo);
            visit.VisitType = TryParseColumn(27, 56, visitInfo);
            visit.InstrumentMode = TryParseColumn(93, 143, visitInfo);
            visit.TargetName = TryParseColumn(145, 176, visitInfo);
            visit.Category = TryParseColumn(178, 208, visitInfo);
            visit.Keywords = TryParseColumn(210, 242, visitInfo);

            // parse date time
            bool parseValid = DateTime.TryParse(visitInfo[58..78].TrimEnd(), out DateTime parsedTime);
            if (parseValid)
            {
                visit.ScheduledStartTime = parsedTime;
            }
            else
            {
                visit.ScheduledStartTime = DateTime.Now;
                //visit.VisitValid = false;
            }

            // parse time span
            parseValid = TimeSpan.TryParse(visitInfo[83..91].TrimEnd(), out TimeSpan parsedDuration);
            if (parseValid)
            {
                visit.Duration = parsedDuration;
            }
            else
            {
                visit.Duration = TimeSpan.Zero;
            }
            visits.Add(visit);
        }
        public void AddVisit(string visitInfo)
        {
            JWSTVisit newVisit = new JWSTVisit();
            string[] chunks = new string[9];
            List<string> splits = visitInfo.Split("  ").ToList();
            splits.RemoveAll(s => s == "");
            for (int i = 0; i < splits.Count; i++)
            {
                chunks[i] = splits[i];
            }

            if (chunks[0] is not null or " ")
            {
                newVisit.VisitID = chunks[0];
            }
            if (chunks[1] is not null or " ")
            {
                newVisit.PCSMode = chunks[1];
            }
            if (chunks[2] is not null or " ")
            {
                newVisit.VisitType = chunks[2];
            }
            if (chunks[3] is not null or " ")
            {
                if (chunks[3].Contains("PRIME"))
                {
                    // attached to prime baby
                    // use datetime of previous visit (it was either in the same group as this one or is the prime)
                    DateTime validTime = visits.LastOrDefault(new JWSTVisit()).ScheduledStartTime;
                    Console.WriteLine("PRIME HANDLED");
                }
                else
                {
                    newVisit.ScheduledStartTime = DateTime.Parse(chunks[3]);
                }
            }
            if (chunks[4] is not null or " ")
            {
                // remove "00/"

                try
                {
                    newVisit.Duration = TimeSpan.Parse(chunks[4].Substring(3));
                }
                catch (Exception)
                {
                    newVisit.Duration = TimeSpan.Zero;
                    Console.WriteLine("DURATION PARSE INVALID");
                }
            }
            if (chunks[5] is not null or " ")
            {
                newVisit.InstrumentMode = chunks[5];
            }
            if (chunks[6] is not null or " ")
            {
                newVisit.TargetName = chunks[6];
            }
            if (chunks[7] is not null or " ")
            {
                newVisit.Category = chunks[7];
            }
            if (chunks[8] is not null or " ")
            {
                newVisit.Keywords = chunks[8];
            }
            visits.Add(newVisit);
        }

        public int GetVisitCount()
        {
            return visits.Count;
        }

        // orders visits so that last visit is first in the list
        public List<JWSTVisit> GetOrderedVisits()
        {
            return visits.OrderByDescending(k => k.ScheduledStartTime).ToList();
        }

        

        private string TryParseColumn(int start, int end, string line)
        {
            // if the start is after the end of the string, this column isnt there, so say none
            if (start >= line.Length) return "none";
            // chunk is the text from the start to the end or the end of the string, whichever is first
            string chunk = line[start..Math.Min(end, line.Length)].TrimEnd();
            // if chunk is null or empty the column is missing
            if (string.IsNullOrEmpty(chunk)) return "none";
            return chunk;
        }
    }
}
