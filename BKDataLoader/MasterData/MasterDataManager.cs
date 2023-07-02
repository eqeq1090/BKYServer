using System;
using System.IO;
using System.Collections.Immutable;
using System.Collections.Generic;
using BKServerBase.Logger;
using BKServerBase.Threading;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Threading;
using System.Linq;
using BKProtocol;
using BKServerBase.Config;
using BKServerBase.Util;
using BKDataLoader.Loader;

namespace BKDataLoader.MasterData
{
    public partial class MasterDataManager : BaseSingleton<MasterDataManager>
    {
        private static object syncRoot = new object();
#if DEBUG
        private static ThreadLocal<bool> m_IsLoading = new ThreadLocal<bool>(false);
#endif

        private delegate void OnLoadCallback<T>(T holder) where T : class;
        private delegate bool OnValidateCallback<T>(T item) where T : class;



        private RegexTableEx[]? _regexTableExs;
        private MasterDataManager()
        {
        }

        public bool Initialize()
        {
            if (LoadDataFrom() == false)
            {
                MiscLog.Critical.LogFatal("[LoadData] LoadDataFrom failed");
                return false;
            }
            return true;
        }

        public bool ReloadData()
        {
            MasterDataManager newMasterData = new MasterDataManager();
            if (newMasterData.LoadDataFrom() == false)
            {
                MiscLog.Critical.LogFatal("[ReloadData] LoadDataFrom failed");
                return false;
            }
            ChangeInstance(newMasterData);
            return true;
        }

        public bool LoadDataFrom()
        {
            try
            {
#if DEBUG
                m_IsLoading.Value = true;
#endif
                return true;
            }
            catch (Exception e)
            {
                MiscLog.Critical.LogFatal(e);
                return false;
            }
            finally
            {
#if DEBUG
                m_IsLoading.Value = false;
#endif
            }
        }
    }

    public class RowItem
    {
        public int Probability { get; set; }
        public int ItemPackID { get; set; }
        public int GroupID { get; set; }
        public int ItemID { get; set; }
        public int CountMin { get; set; }
        public int CountMax { get; set; }
        public int ProbKeep { get; set; }
        public int ProbShow { get; set; }
        public int Lucky { get; set; }
    }
    public record TrophyRewardInfo
    {
        public int Id;
        public int TrophyMin;
        public int TrophyMax;
        public Dictionary<int, int> TrophyRewardMap = new();
    }

    public struct RegexTableEx
    {
        public Regex regex;
    }
}