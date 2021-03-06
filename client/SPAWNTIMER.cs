﻿using Structures;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace myseq
{
    public class SPAWNTIMER
    {
        public SPAWNTIMER(SPAWNINFO si, DateTime dt)

        {
            SpawnLoc=si.SpawnLoc;

            ZoneSpawnLoc = si.ZoneSpawnLoc;

            zone = "";

            X = si.X;

            Y = si.Y;

            Z = si.Z;

            SpawnTimeDT = dt;

            SpawnTimeStr = dt.ToLongTimeString() + " " + dt.ToShortDateString();

            SpawnCount = 1;

            LastSpawnName = si.Name;

            AllNames = RegexHelper.TrimName(si.Name);
        }

        public SPAWNTIMER(string str)

        {
            string[] parts = str.Split(';');

            SpawnLoc = parts[0];

            SpawnCount = int.Parse(parts[1]);

            SpawnTimer = int.Parse(parts[2]);

            SpawnTimeStr = parts[3];

            if (SpawnTimeStr.Length>0) SpawnTimeDT = Convert.ToDateTime(SpawnTimeStr);

            KillTimeStr = parts[4];

            if (KillTimeStr.Length>0) KillTimeDT = Convert.ToDateTime(KillTimeStr);

            NextSpawnStr = parts[5];

            if (NextSpawnStr.Length>0) NextSpawnDT = Convert.ToDateTime(NextSpawnStr);

            LastSpawnName = parts[6];

            AllNames = parts[7];

            // split up the All Names, and parse out spawn numbers in parenthesis
            string[] subparts = AllNames.Split(',');
            _ = subparts.GetUpperBound(0);

            X = float.Parse(parts[8]);

            Y = float.Parse(parts[9]);

            Z = float.Parse(parts[10]);
        }

        // Returns the data in a format which can be used with the (string) constructor.

        public string GetAsString() => SpawnLoc + ";" + SpawnCount + ";" + SpawnTimer + ";" + SpawnTimeStr + ";" + KillTimeStr + ";" +

            NextSpawnStr + ";" + LastSpawnName + ";" + AllNames + ";" + X + ";" + Y + ";" + Z;

        // st has been loaded from a file, and is the same spawn as "this" one. 

        // Glean all useful information.

        public void Merge(SPAWNTIMER st)

        {
            LogLib.WriteLine("Merging spawn timers:",LogLevel.Debug);

            LogLib.WriteLine($"  Old: {GetAsString()}", LogLevel.Debug);

            LogLib.WriteLine($"  Other: {st.GetAsString()}", LogLevel.Debug);

            SpawnCount = st.SpawnCount; // usually makes it > 1

            SpawnTimer = st.SpawnTimer; // woot!

            //this.SpawnTimer =   // NOT!

            if (KillTimeDT == DateTime.MinValue) // woot!

            {
                KillTimeStr = st.KillTimeStr;

                TimeSpan Diff = new TimeSpan(0, 0, 0, Convert.ToInt32(SpawnTimer));

                NextSpawnDT = KillTimeDT.Add(Diff);

                NextSpawnStr = NextSpawnDT.ToLongTimeString() + " " + NextSpawnDT.ToShortDateString();
            }
            else
            {
                // Enable the timer to start on first kill

                if (st.SpawnTimer > 10)

                {
                    TimeSpan Diff = new TimeSpan(0, 0, 0, Convert.ToInt32(SpawnTimer));

                    if (DateTime.Now.Subtract(Diff) < st.SpawnTimeDT)

                    {
                        SpawnTimeDT = st.SpawnTimeDT;

                        SpawnTimeStr = st.SpawnTimeStr;
                    }

                    if (DateTime.Now.Subtract(Diff) > st.KillTimeDT)

                    {
                        KillTimeDT = DateTime.MinValue;
                    }
                    else
                    {
                        KillTimeDT = st.KillTimeDT;

                        KillTimeStr = st.KillTimeStr;
                    }

                    if (DateTime.Now > st.NextSpawnDT)

                    {
                        NextSpawnDT = DateTime.MinValue;

                        NextSpawnStr = "";
                    }
                    else
                    {
                        NextSpawnDT = st.NextSpawnDT;

                        NextSpawnStr = st.NextSpawnStr;

                        KillTimeDT = st.KillTimeDT.Subtract(Diff);
                    }
                }
            }

            int namecount = 1;

            foreach (var name in st.AllNames.Split(','))
            {
                var bname = RegexHelper.TrimName(name);
                if (AllNames.IndexOf(bname) < 0 && namecount < 11)
                {
                    AllNames += ", " + bname;
                    namecount++;
                }
            }

            // update last spawn name to be what looks like named mobs
            foreach (var tname in AllNames.Split(','))
            {
                var mname = RegexHelper.TrimName(tname);
                if (RegexHelper.RegexMatch(mname))
                {
                    LastSpawnName = mname;
                    break;
                }
            }

            listNeedsUpdate = true;
            LogLib.WriteLine($"  New: {GetAsString()}", LogLevel.Debug);
        }

        // When will the mob spawn next? Returns 0 if not available.

        // TODO: optimize this, as it is called much more often than the mob is being updated

        public int SecondsUntilSpawn(DateTime now)

        {
            int checkTimer=0;

            if (NextSpawnDT != DateTime.MinValue)

            {
                TimeSpan Diff = NextSpawnDT.Subtract(now);

                checkTimer = (Diff.Hours * 3600) + (Diff.Minutes * 60) + Diff.Seconds;

                if (checkTimer<=0)

                {
                    checkTimer=0;
                }
            }

            return checkTimer;
        }

        public string GetDescription()

        {
            int countTime = 0;

            string countTimer = "";

            if (NextSpawnDT != DateTime.MinValue) {
                TimeSpan Diff = NextSpawnDT.Subtract(DateTime.Now);

                countTimer = Diff.Hours.ToString("00") + ":" + Diff.Minutes.ToString("00") + ":" + Diff.Seconds.ToString("00");

                countTime = (Diff.Hours * 3600) + (Diff.Minutes * 60) + Diff.Seconds;
            }

            if (countTime > 0)

            {
                // StringBuilder moved to new, common method, as equal for all paths.
                StringBuilder spawnTimer = StBuilder();

                spawnTimer.Append("\n");

                spawnTimer.AppendFormat("Last Spawned At: {0}\n", SpawnTimeStr);

                spawnTimer.AppendFormat("Last Killed At: {0}\n", KillTimeStr);

                spawnTimer.AppendFormat("Next Spawn At: {0}\n", NextSpawnStr);

                spawnTimer.AppendFormat("Spawn Timer: {0} secs\n", SpawnTimer);

                spawnTimer.AppendFormat("Spawning In: {0}\n", countTimer);

                spawnTimer.AppendFormat("Spawn Count: {0}\n", SpawnCount);

                spawnTimer.AppendFormat("Y: {0:f3}  X: {1:f3}  Z: {2:f3}", Y, X, Z);

                return spawnTimer.ToString();
            }
            else if (SpawnTimer > 0)

            {
                StringBuilder spawnTimer = StBuilder();

                spawnTimer.Append("\n");

                spawnTimer.AppendFormat("Last Spawned At: {0}\n", SpawnTimeStr);

                spawnTimer.AppendFormat("Last Killed At: {0}\n", KillTimeStr);

                spawnTimer.AppendFormat("Next Spawn At: {0}\n", "");

                spawnTimer.AppendFormat("Spawn Timer: {0} secs\n", SpawnTimer);

                spawnTimer.AppendFormat("Spawning In: {0}\n", "");

                spawnTimer.AppendFormat("Spawn Count: {0}\n", SpawnCount);

                spawnTimer.AppendFormat("Y: {0:f3}  X: {1:f3}  Z: {2:f3}", Y, X, Z);

                return spawnTimer.ToString();
            }
            else
            {
                StringBuilder spawnTimer = StBuilder();

                spawnTimer.Append("\n");

                spawnTimer.AppendFormat("Last Spawned At: {0}\n", SpawnTimeStr);

                spawnTimer.AppendFormat("Last Killed At: {0}\n", KillTimeStr);

                spawnTimer.AppendFormat("Next Spawn At: {0}\n", "");

                spawnTimer.AppendFormat("Spawn Timer: {0} secs\n", "0");

                spawnTimer.AppendFormat("Spawning In: {0}\n", "");

                spawnTimer.AppendFormat("Spawn Count: {0}\n", SpawnCount);

                spawnTimer.AppendFormat("Y: {0:f3}  X: {1:f3}  Z: {2:f3}", Y, X, Z);

                return spawnTimer.ToString();
            }
        }

        private StringBuilder StBuilder()
        {
            StringBuilder spawnTimer = new StringBuilder();

            spawnTimer.AppendFormat("Spawn Name: {0}\n", LastSpawnName);

            string names_to_add = "Names encountered: ";
            string[] names = AllNames.Split(',');

            int namecount = 0;

            NameCount(spawnTimer, ref names_to_add, names, ref namecount);

            if (names_to_add.Length > 0)
            {
                spawnTimer.Append(names_to_add);
            }

            return spawnTimer;
        }

        private static void NameCount(StringBuilder spawnTimer, ref string names_to_add, string[] names, ref int namecount)
        {
            foreach (string name in names)
            {
                var namet = RegexHelper.TrimName(name);

                if (namecount == 0)
                {
                    names_to_add += namet;
                }
                else
                {
                    if ((namet.Length + names_to_add.Length + 2) < 45)
                    {
                        names_to_add += ", ";
                        names_to_add += namet;
                    }
                    else
                    {
                        spawnTimer.Append(names_to_add);
                        spawnTimer.Append("\n");
                        names_to_add = namet;
                    }
                }

                namecount++;
            }
        }

        // A true re-spawn has been detected        

        public string ReSpawn(string name)

        {
            string log = "";

            try

            {
                SpawnCount++;

                // if it looks like a named, leave last spawn name alone
                if (LastSpawnName.Length > 0)
                {
                    // See if mob name starts with capital letter or #
                    if (!RegexHelper.RegexMatch(LastSpawnName))
                    {
                        LastSpawnName = name;
                    }
                }
                else
                {
                    LastSpawnName = name;
                }
                NextSpawnStr = "";

                NextSpawnDT = DateTime.MinValue;

                SpawnTimeDT = DateTime.Now;

                SpawnTimeStr = SpawnTimeDT.ToLongTimeString() + " " + SpawnTimeDT.ToShortDateString();

                // put name at beginning of list of AllNames

                var newnames = RegexHelper.TrimName(name);
                var namecount = 1;
                foreach (var tname in AllNames.Split(','))
                {
                    var bname = RegexHelper.TrimName(tname);
                    if (newnames.IndexOf(bname) < 0 && namecount < 12)
                    {
                        newnames += ", " + bname;
                        namecount++;
                    }
                }

                AllNames = newnames;

                if (KillTimeDT != DateTime.MinValue)

                {
                    // This mob has been killed already - now we can calculate

                    // the respawn time

                    int last_Timer = SpawnTimer;

                    TimeSpan Diff = SpawnTimeDT.Subtract(KillTimeDT);

                    SpawnTimer = (Diff.Hours * 3600) + (Diff.Minutes * 60) + Diff.Seconds;

                    if (Settings.Instance.MaxLogLevel > 0)
                    {
                        string spawnTimer = $"{Diff.Hours:00}:{Diff.Minutes:00}:{Diff.Seconds:00}";

                        log = $"Setting Timer for Spawn: {SpawnLoc} Name: {name} Count: {SpawnCount} Last Kill Time: {KillTimeStr} Current Spawn Time: {SpawnTimeStr} Timer: {spawnTimer} = {SpawnTimer} secs Old: {last_Timer} secs";
                    }

                    // ... and forget about the kill

                    KillTimeDT = DateTime.MinValue;

                    KillTimeStr = "";
                }
            }
            catch (Exception ex) {LogLib.WriteLine("Error updating Timer SPAWNTIMER for " + name + ": ", ex);}

            listNeedsUpdate = true;

            return log;
        }

        public string Kill(DateTime dt)

        {
            KillTimeDT = dt;

            KillTimeStr = dt.ToLongTimeString() + " " + dt.ToShortDateString();

            TimeSpan Diff = new TimeSpan(0, 0, 0, Convert.ToInt32(SpawnTimer));

            NextSpawnDT = KillTimeDT.Add(Diff);

            NextSpawnStr = NextSpawnDT.ToLongTimeString() + " " + NextSpawnDT.ToShortDateString();

            listNeedsUpdate = true;

            return KillTimeStr;
        }

        public ListViewItem GetListItem()

        {
            bool isInList = true;

            if (itmSpawnTimerList == null)

            {
                itmSpawnTimerList = new ListViewItem(RegexHelper.FixMobName(LastSpawnName));

                isInList = false;

                listNeedsUpdate = true;

                for (int t=0; t<10; t++)

                {
                    itmSpawnTimerList.SubItems.Add("");
                }
            }

            SpawnTimeRemaining = SecondsUntilSpawn(DateTime.Now);

            if (SpawnTimeRemaining < 1 || SpawnTimeRemaining > 120)
                itmSpawnTimerList.ForeColor = Color.Black;
            else if (SpawnTimeRemaining < 30)
                itmSpawnTimerList.ForeColor = Color.Red;
            else if (SpawnTimeRemaining < 60)
                itmSpawnTimerList.ForeColor = Color.IndianRed;
            else itmSpawnTimerList.ForeColor = SpawnTimeRemaining < 90 ? Color.Orange : Color.Goldenrod;

            if (listNeedsUpdate)
            {
                listNeedsUpdate = false;

                itmSpawnTimerList.SubItems[1].Text = SpawnTimeRemaining.ToString();

                itmSpawnTimerList.SubItems[2].Text = SpawnTimer.ToString();

                itmSpawnTimerList.SubItems[3].Text = zone;

                itmSpawnTimerList.SubItems[4].Text = X.ToString();

                itmSpawnTimerList.SubItems[5].Text = Y.ToString();

                itmSpawnTimerList.SubItems[6].Text = Z.ToString();

                itmSpawnTimerList.SubItems[7].Text = SpawnCount.ToString();

                itmSpawnTimerList.SubItems[8].Text = SpawnTimeStr;

                itmSpawnTimerList.SubItems[9].Text = KillTimeStr;

                itmSpawnTimerList.SubItems[10].Text = NextSpawnStr;
            }
            else
            {
                if (SpawnTimeRemaining.ToString() != itmSpawnTimerList.SubItems[1].Text)
                    itmSpawnTimerList.SubItems[1].Text = SpawnTimeRemaining.ToString();
            }

            if (!isInList)

            {
                return itmSpawnTimerList;
            }
            else
            {
                return null; // it already is in the list - don't add it again
            }
        }

        public string ZoneSpawnLoc;

        public string SpawnLoc;            // x,y = primary key, set on first spawn

        public string zone;

        public bool sticky = false;

        public float Y = 0;

        public float X = 0;

        public float Z = 0;

        public bool filtered = false;

        public int SpawnCount = 0;          // Updated on true re-spawn

        public int SpawnTimeRemaining = 0;

        public int SpawnTimer = 0;          // Updated on true re-spawn

        public string SpawnTimeStr;    // Update on spawn (last spawn time)

        public DateTime SpawnTimeDT = DateTime.MinValue;

        public string KillTimeStr = "";     // Updated on each kill, erased on spawn

        public DateTime KillTimeDT = DateTime.MinValue;

        public string NextSpawnStr = "";    // Updated on each kill, erased on spawn

        public DateTime NextSpawnDT = DateTime.MinValue;

        public string LastSpawnName = "";   // Updated on each spawn

        public ListViewItem itmSpawnTimerList = null;

        private bool listNeedsUpdate = false;

        public string AllNames { get; set; } = "";
    }
}

