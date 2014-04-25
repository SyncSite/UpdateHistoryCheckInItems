using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UpdateHistoryCheckInItems
{
    class Program
    {
        public static Regex captureZero = new Regex(@"Checked In at (?<Name>.+) from 0 miles away.(?<Rest>.*)", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string replacementString = "Checked In at {0} from {1} miles away.{2}";

        static void Main(string[] args)
        {
            int count = 0;
            using (var context = new SaleslogixDataContext())
            {
                var histories = from h in context.HISTORies
                                where h.NOTES.Contains(" 0 ")
                                      && h.LONGITUDE.HasValue && h.LATITUDE.HasValue &&
                                      h.ACCOUNT.ADDRESS.LONGITUDE.HasValue && h.ACCOUNT.ADDRESS.LATITUDE.HasValue
                                select h;
                foreach (HISTORY history in histories)
                {
                    try
                    {
                        // Verify that the 0 is the correct one to replace.
                        if ((history.LONGNOTES != null && captureZero.IsMatch(history.LONGNOTES)) || (history.NOTES != null && captureZero.IsMatch(history.NOTES)))
                        {
                            Console.WriteLine("Correcting History Record " + history.HISTORYID);
                            Match match = (history.LONGNOTES != null) ? captureZero.Match(history.LONGNOTES) : captureZero.Match(history.NOTES);
                            string name = match.Groups["Name"].Value;
                            string rest = match.Groups["Rest"].Value;

                            double lat1 = history.LATITUDE.Value;
                            double long1 = history.LONGITUDE.Value;
                            double lat2 = (double)history.ACCOUNT.ADDRESS.LATITUDE.Value;
                            double long2 = (double)history.ACCOUNT.ADDRESS.LONGITUDE.Value;
                            // Calculate Distance
                            double distance = context.fnGetDistance(lat1, long1,
                                                                    lat2, long2, "miles").Value;
                            Console.WriteLine(string.Format("    New distance will be {0} miles.", distance.ToString("#0.00")));
                            string longNotes = string.Format(replacementString, name, distance.ToString("#0.00"), rest);
                            string notes = string.Empty;
                            notes = notes.Length > 255 ? notes.Substring(0, 254) : longNotes;
                            Console.WriteLine(string.Format("    Old Notes : {0}", history.LONGNOTES));
                            Console.WriteLine(string.Format("    New Notes : {0}", notes));
                            // Update Notes with new text.
                            history.NOTES = notes;
                            history.LONGNOTES = longNotes;
                            
                            count++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error in processing : " + ex);
                    }
                }

                Console.WriteLine(string.Format("Correcting {0} Records", count));

                context.SubmitChanges();

                Console.WriteLine("Finished");
                System.Threading.Thread.Sleep(5000);
            }
        }
    }
}
