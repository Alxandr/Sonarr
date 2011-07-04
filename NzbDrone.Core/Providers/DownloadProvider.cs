﻿using System;
using Ninject;
using NLog;
using NzbDrone.Core.Model;
using NzbDrone.Core.Repository;

namespace NzbDrone.Core.Providers
{
    public class DownloadProvider
    {
        private readonly SabProvider _sabProvider;
        private readonly HistoryProvider _historyProvider;
        private readonly EpisodeProvider _episodeProvider;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Inject]
        public DownloadProvider(SabProvider sabProvider, HistoryProvider historyProvider, EpisodeProvider episodeProvider)
        {
            _sabProvider = sabProvider;
            _historyProvider = historyProvider;
            _episodeProvider = episodeProvider;
        }

        public DownloadProvider()
        {
        }

        public virtual bool DownloadReport(EpisodeParseResult parseResult)
        {
            var sabTitle = _sabProvider.GetSabTitle(parseResult);

            if (_sabProvider.IsInQueue(sabTitle))
            {
                Logger.Warn("Episode {0} is already in sab's queue. skipping.", parseResult);
                return false;
            }

            var addSuccess = _sabProvider.AddByUrl(parseResult.NzbUrl, sabTitle);

            if (addSuccess)
            {
                foreach (var episode in _episodeProvider.GetEpisodesByParseResult(parseResult))
                {
                    var history = new History();
                    history.Date = DateTime.Now;
                    history.Indexer = parseResult.Indexer;
                    history.IsProper = parseResult.Quality.Proper;
                    history.Quality = parseResult.Quality.QualityType;
                    history.NzbTitle = parseResult.NzbTitle;
                    history.EpisodeId = episode.EpisodeId;
                    history.SeriesId = episode.SeriesId;

                    _historyProvider.Add(history);
                    _episodeProvider.MarkEpisodeAsFetched(episode.EpisodeId);
                }
            }

            return addSuccess;
        }
    }
}