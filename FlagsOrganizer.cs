﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PKHeX.Core;

namespace MissingEventFlagsCheckerPlugin
{
    public abstract class FlagsOrganizer
    {
        public enum FlagType
        {
            FieldItem,
            HiddenItem,
            TrainerBattle,
            StationaryBattle,
            InGameTrade,
            Gift,
            GeneralEvent,
            SideEvent,
            StoryEvent,
            BerryTree,
        }

        protected class FlagDetail
        {
            public int OrderKey { get; private set; }
            public int FlagIdx { get; private set; }
            public string FlagTypeTxt { get; private set; }
            public string LocationName { get; private set; }
            public string DetailMsg { get; private set; }
            public bool IsSet { get; set; }


            public FlagDetail(string detailEntry)
            {
                string[] info = detailEntry.Split('\t');

                if (info.Length < 6)
                {
                    throw new ArgumentException("Argmument detailEntry format is not valid");
                }

                OrderKey = string.IsNullOrWhiteSpace(info[0]) ? int.MaxValue : Convert.ToInt32(info[0]);
                FlagIdx = Convert.ToInt32(info[1], 16);
                FlagTypeTxt = info[2];
                LocationName = info[3];
                if (!string.IsNullOrWhiteSpace(info[4]))
                {
                    LocationName += " " + info[4];
                }
                DetailMsg = info[5];
                IsSet = false;
            }

            public FlagDetail(int flagIdx, FlagType flagType, string detailMsg) : this(flagIdx, flagType, "", detailMsg)
            {
            }

            public FlagDetail(int flagIdx, FlagType flagType, string locationName, string detailMsg)
            {
                //OrderKey = int.MaxValue;
                OrderKey = flagIdx;
                FlagIdx = flagIdx;

                switch (flagType)
                {
                    case FlagType.FieldItem:
                        FlagTypeTxt = "FIELD ITEM";
                        break;

                    case FlagType.HiddenItem:
                        FlagTypeTxt = "HIDDEN ITEM";
                        break;

                    case FlagType.TrainerBattle:
                        FlagTypeTxt = "TRAINER BATTLE";
                        break;

                    case FlagType.StationaryBattle:
                        FlagTypeTxt = "STATIONARY BATTLE";
                        break;

                    case FlagType.InGameTrade:
                        FlagTypeTxt = "IN-GAME TRADE";
                        break;

                    case FlagType.Gift:
                        FlagTypeTxt = "GIFT";
                        break;

                    case FlagType.GeneralEvent:
                        FlagTypeTxt = "EVENT";
                        break;

                    case FlagType.SideEvent:
                        FlagTypeTxt = "SIDE EVENT";
                        break;

                    case FlagType.StoryEvent:
                        FlagTypeTxt = "STORY EVENT";
                        break;

                    case FlagType.BerryTree:
                        FlagTypeTxt = "BERRY TREE";
                        break;
                }

                LocationName = locationName;
                DetailMsg = detailMsg;
            }

            public override string ToString()
            {
                if (string.IsNullOrEmpty(LocationName))
                {
                    return string.Format("{0} - {1}\r\n", FlagTypeTxt, DetailMsg);
                }

                else
                {
                    return string.Format("{0} - {1} - {2}\r\n", FlagTypeTxt, LocationName, DetailMsg);
                }
            }
        }


        protected SaveFile m_savFile;
        protected bool[] m_eventFlags;

        protected List<FlagDetail> m_missingEventFlagsList = new List<FlagDetail>(4096);

        //temp
        protected bool isAssembleChecklist = false;

        protected abstract void InitFlagsData(SaveFile savFile);

        protected abstract void CheckAllMissingFlags();

        protected virtual void AssembleChecklist()
        {
            isAssembleChecklist = true;
            CheckAllMissingFlags();
            isAssembleChecklist = false;
        }

        public virtual void ExportMissingFlags()
        {
            CheckAllMissingFlags();
            m_missingEventFlagsList.Sort((x, y) => x.OrderKey - y.OrderKey);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < m_missingEventFlagsList.Count; ++i)
            {
                if (!m_missingEventFlagsList[i].IsSet)
                {
                    sb.Append(m_missingEventFlagsList[i]);
                }
            }

            System.IO.File.WriteAllText(string.Format("missing_events_{0}.txt", m_savFile.Version), sb.ToString());
        }

        public virtual void ExportChecklist()
        {
            AssembleChecklist();
            m_missingEventFlagsList.Sort((x, y) => x.OrderKey - y.OrderKey);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < m_missingEventFlagsList.Count; ++i)
            {
                sb.AppendFormat("[{0}] {1}", m_missingEventFlagsList[i].IsSet ? "x" : " ", m_missingEventFlagsList[i]);
            }

            System.IO.File.WriteAllText(string.Format("checklist_{0}.txt", m_savFile.Version), sb.ToString());
        }

        public virtual void DumpAllFlags()
        {
            StringBuilder sb = new StringBuilder(m_eventFlags.Length);

            for (int i = 0; i < m_eventFlags.Length; ++i)
            {
                sb.AppendFormat("FLAG_0x{0:X4} {1}\r\n", i, m_eventFlags[i]);
            }

            System.IO.File.WriteAllText(string.Format("flags_dump_{0}.txt", m_savFile.Version), sb.ToString());
        }

        public virtual void MarkFlags(FlagType flagType) { }
        public virtual void UnmarkFlags(FlagType flagType) { }
        public virtual bool SupportsEditingFlag(FlagType flagType)
        {
            switch (flagType)
            {
                case FlagType.FieldItem:
                case FlagType.HiddenItem:
                case FlagType.TrainerBattle:
                    return true;

                default:
                    return false;
            }
        }

        protected void CheckMissingFlag(int flagIdx, FlagType flagType, string mapLocation, string flagDetail)
        {
            if (isAssembleChecklist)
            {
                m_missingEventFlagsList.Add(new FlagDetail(flagIdx, flagType, mapLocation, flagDetail) { IsSet = IsFlagSet(flagIdx) });
            }

            else if (!IsFlagSet(flagIdx))
            {
                m_missingEventFlagsList.Add(new FlagDetail(flagIdx, flagType, mapLocation, flagDetail));
            }
        }

        protected bool IsFlagSet(int flagIdx) => m_eventFlags[flagIdx];


        protected string ReadFlagsListRes(string resName)
        {
            string contentTxt = null;

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            resName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resName));

            using (var stream = assembly.GetManifestResourceStream(resName))
            {
                using (var reader = new System.IO.StreamReader(stream))
                {
                    contentTxt = reader.ReadToEnd();
                }
            }

            return contentTxt;
        }



        public static FlagsOrganizer OrganizeFlags(SaveFile savFile)
        {
            FlagsOrganizer flagsOrganizer = null;

            switch (savFile.Version)
            {
                case GameVersion.Any:
                case GameVersion.RBY:
                case GameVersion.StadiumJ:
                case GameVersion.Stadium:
                case GameVersion.Stadium2:
                case GameVersion.RSBOX:
                case GameVersion.COLO:
                case GameVersion.XD:
                case GameVersion.CXD:
                case GameVersion.BATREV:
                case GameVersion.ORASDEMO:
                case GameVersion.GO:
                case GameVersion.Unknown:
                case GameVersion.Invalid:
                    break; // unsupported format

                case GameVersion.RD:
                case GameVersion.GN:
                case GameVersion.RB:
                    flagsOrganizer = new FlagsGen1RB();
                    break;

                case GameVersion.YW:
                    flagsOrganizer = new FlagsGen1Y();
                    break;

                case GameVersion.GD:
                case GameVersion.SI:
                case GameVersion.GS:
                    flagsOrganizer = new FlagsGen2GS();
                    break;

                case GameVersion.C:
                    flagsOrganizer = new FlagsGen2C();
                    break;

                case GameVersion.R:
                case GameVersion.S:
                case GameVersion.RS:
                    flagsOrganizer = new FlagsGen3RS();
                    break;

                case GameVersion.FR:
                case GameVersion.LG:
                case GameVersion.FRLG:
                    flagsOrganizer = new FlagsGen3FRLG();
                    break;

                case GameVersion.E:
                    flagsOrganizer = new FlagsGen3E();
                    break;

                case GameVersion.D:
                case GameVersion.P:
                case GameVersion.DP:
                    flagsOrganizer = new FlagsGen4DP();
                    break;

                case GameVersion.Pt:
                    flagsOrganizer = new FlagsGen4Pt();
                    break;

                case GameVersion.HG:
                case GameVersion.SS:
                case GameVersion.HGSS:
                    flagsOrganizer = new FlagsGen4HGSS();
                    break;

                case GameVersion.B:
                case GameVersion.W:
                case GameVersion.BW:
                    flagsOrganizer = new FlagsGen5BW();
                    break;

                case GameVersion.B2:
                case GameVersion.W2:
                case GameVersion.B2W2:
                    flagsOrganizer = new FlagsGen5B2W2();
                    break;

                case GameVersion.X:
                case GameVersion.Y:
                case GameVersion.XY:
                    flagsOrganizer = new FlagsGen6XY();
                    break;

                case GameVersion.OR:
                case GameVersion.AS:
                case GameVersion.ORAS:
                    flagsOrganizer = new FlagsGen6ORAS();
                    break;

                case GameVersion.SN:
                case GameVersion.MN:
                case GameVersion.SM:
                    flagsOrganizer = new FlagsGen7SM();
                    break;

                case GameVersion.US:
                case GameVersion.UM:
                case GameVersion.USUM:
                    flagsOrganizer = new FlagsGen7USUM();
                    break;

                case GameVersion.GP:
                case GameVersion.GE:
                case GameVersion.GG:
                    flagsOrganizer = new FlagsGen7bGPGE();
                    break;

                case GameVersion.BD:
                case GameVersion.SP:
                case GameVersion.BDSP:
                    flagsOrganizer = new FlagsGen8bsBDSP();
                    break;




                case GameVersion.SW:
                case GameVersion.SH:
                case GameVersion.SWSH:
                case GameVersion.PLA:
                case GameVersion.SL:
                case GameVersion.VL:
                case GameVersion.SV:
                    flagsOrganizer = new DummyOrgBlockFlags();
                    break;

                default:
                    break;
            }

            if (flagsOrganizer != null)
            {
                flagsOrganizer.InitFlagsData(savFile);
            }

            return flagsOrganizer;
        }

    }



    //TEMP
    class DummyOrgFlags : FlagsOrganizer
    {
        protected override void CheckAllMissingFlags() { }
        protected override void InitFlagsData(SaveFile savFile)
        {
            m_savFile = savFile;
            m_eventFlags = (m_savFile as IEventFlagArray).GetEventFlags();
            m_missingEventFlagsList.Clear();
        }

        public override void ExportMissingFlags() { }
        public override void ExportChecklist() { }
        public override bool SupportsEditingFlag(FlagType flagType) { return false; }
    }

    class DummyOrgBlockFlags : FlagsOrganizer
    {
        Dictionary<uint, bool> m_blockEventFlags;

        protected override void CheckAllMissingFlags() { }
        protected override void InitFlagsData(SaveFile savFile)
        {
            m_savFile = savFile;
            m_eventFlags = new bool[0]; // dummy

            m_blockEventFlags = new Dictionary<uint, bool>();
            foreach (var b in (m_savFile as ISCBlockArray).AllBlocks)
            {
                if (b.Type == SCTypeCode.Bool1 || b.Type == SCTypeCode.Bool2)
                {
                    m_blockEventFlags.Add(b.Key, (b.Type == SCTypeCode.Bool2));
                }
            }

            m_missingEventFlagsList.Clear();
        }

        public override void ExportMissingFlags() { }

        public override void ExportChecklist() { }
        public override bool SupportsEditingFlag(FlagType flagType) { return false; }

        public override void DumpAllFlags()
        {
            StringBuilder sb = new StringBuilder(m_blockEventFlags.Count);

            var keys = new List<uint>(m_blockEventFlags.Keys);
            for (int i = 0; i < keys.Count; ++i)
            {
                sb.AppendFormat("FLAG_0x{0:X8} {1}\r\n", keys[i], m_blockEventFlags[keys[i]]);
            }

            System.IO.File.WriteAllText(string.Format("flags_dump_{0}.txt", m_savFile.Version), sb.ToString());
        }
    }
}
