namespace SampleApiSample.Controllers
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using Microsoft.AspNetCore.Mvc;

	[Route("api/[controller]")]
    public class SampleDataController : Controller
    {
		public class SearchResults
		{
			public long count { get; set; }
			public long total { get; set; }
			public IEnumerable<SearchResult> results { get; set; }
		}

		public class SearchResult
		{
			public string id { get; set; }
			public string title { get; set; }
			public string duration { get; set; }
			public string url { get; set; }
			public long reach { get; set; }
		}

		[HttpGet("[action]")]
		public int GetVideoReach([FromQuery] string id)
		{
			var data = SubSplashSim.Fetch(null, null, id, "reach");

			if (data.count == 1)
			{
				return data._embedded.mediaitems[0].reach;
			}

			throw new KeyNotFoundException();
		}
		
		[HttpGet("[action]")]
        public SearchResults GetVideos([FromQuery] int? pageSize, [FromQuery] int? pageNum, [FromQuery] int? minDuration )
        {
			List<SearchResult> results = new List<SearchResult>();

			// Fetch the data from subsplash
			var data = SubSplashSim.Fetch(pageSize, pageNum, null, "title,youtube_url,published_at");
			var youTube = Program.getYouTubeService();

			Parallel.ForEach(
				data._embedded.mediaitems,
				new ParallelOptions() { MaxDegreeOfParallelism = 10 },
				item =>
			{
				var search = youTube.Videos.List("snippet,contentDetails"); //.Search.List("snippet,contentDetails");
				search.Id = item.id;
				search.MaxResults = 1;

				var youTubeResult = search.Execute();
				var youTubeItem = youTubeResult.Items[0];

				var groups = Regex.Match(
					youTubeItem.ContentDetails.Duration,
					@"PT(\d+H)?(\d+M)?(\d+S)?").Groups;

				var trimChars = new char[] { 'H', 'M', 'S' };
				var duration = new TimeSpan(0,
					string.IsNullOrWhiteSpace(groups[1].Value) ? 0 : int.Parse(groups[1].Value.TrimEnd(trimChars)),
					string.IsNullOrWhiteSpace(groups[2].Value) ? 0 : int.Parse(groups[2].Value.TrimEnd(trimChars)),
					string.IsNullOrWhiteSpace(groups[3].Value) ? 0 : int.Parse(groups[3].Value.TrimEnd(trimChars)));

				if (!minDuration.HasValue || duration.TotalMinutes >= minDuration.Value)
				{
					results.Add(new SearchResult()
					{
						id = item.id,
						title = item.title, // youTubeItem != null ? youTubeItem.Snippet.Title : null,
						duration = duration.ToString(),
						url = string.Format("https://www.youtube.com/watch?v={0}", item.id),
						reach = 0
					});
				}
			});

			return new SearchResults()
			{
				count = results.Count(),
				total = data.total,
				results = results
			};
		}
    }
}
