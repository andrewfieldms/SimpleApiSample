namespace SampleApiSample
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Google.Apis.YouTube.v3;
	using Newtonsoft.Json;

	public static class SubSplashSim
	{
		static SubSplashSim()
		{
			string result = CreateSampleData(400);
			File.WriteAllText("challengeSample2.json", result);
		}

		public static SubSplashResponse Fetch(int? pageSize, int? pageNum, string filterId, string fields)
		{
			List<string> selectedFields;

			// Set defaults
			if (!pageSize.HasValue) pageSize = 20;
			if (!pageNum.HasValue) pageNum = 1;
			if (fields == null) fields = string.Empty;

			selectedFields = fields.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

			// Load the sample dataset
			string sampleContent = File.ReadAllText("challengeSample.json");
			SubSplashResponse sampleSet = JsonConvert.DeserializeObject<SubSplashResponse>(sampleContent);
			var items = sampleSet._embedded.mediaitems;

			var filtered = string.IsNullOrWhiteSpace(filterId) ? items : items.Where(item => item.id == filterId);

			// Scan forward to the desired page and select the desired count
			var starting = filtered.Skip(pageSize.Value * (pageNum.Value - 1));

			// Scan forward to the desired page and select the desired count
			var selected = starting.Take(pageSize.Value);

			// Fill in the desired response fields
			SubSplashResponse response = new SubSplashResponse();
			response.count = selected.Count();
			response.total = starting.Count();
			response._embedded = new SubSplashResponse.embedded();
			response._embedded.mediaitems = selected.Select(item =>
			{
				return new SubSplashResponse.mediaItem()
				{
					id = item.id,
					title = selectedFields.Count() == 0 || selectedFields.Contains("title") ? item.title : null,
					youtube_url = selectedFields.Count() == 0 || selectedFields.Contains("youtube_url") ? item.youtube_url : null,
					reach = selectedFields.Count() == 0 || selectedFields.Contains("reach") ? item.reach : 0,
					published_at = selectedFields.Count() == 0 || selectedFields.Contains("published_at") ? item.published_at : null
				};
			}).ToArray();

			// Delay the response for 500-1000 ms per requested document
			if (selectedFields.Count() == 0 || selectedFields.Contains("reach"))
			{
				Random rand = new Random();
				selected.ToList().ForEach(item => System.Threading.Thread.Sleep(rand.Next(500, 1000)));
			}

			return response;
		}

		public static string CreateSampleData(long size)
		{
			Random rand = new Random();
			List<SubSplashResponse.mediaItem> mediaItems = new List<SubSplashResponse.mediaItem>();

			var youTube = Program.getYouTubeService();
			var search = youTube.Videos.List("snippet"); //.Search.List("snippet,contentDetails");
			search.Chart = VideosResource.ListRequest.ChartEnum.MostPopular;
			search.MaxResults = (size - mediaItems.Count) < 50 ? (size - mediaItems.Count) : 50;

			while (mediaItems.Count < size)
			{
				var result = search.Execute();
				
				foreach (var i in result.Items.ToList())
				{
					mediaItems.Add(new SubSplashResponse.mediaItem()
					{
						id = i.Id,
						title = i.Snippet.Title,
						youtube_url = "https://www.youtube.com/watch?v=" + i.Id,
						published_at = DateTime.Today.AddDays(-rand.Next(90)).ToString(),
						reach = rand.Next(10000)
					});
				}

				search.PageToken = result.NextPageToken;
			}

			return JsonConvert.SerializeObject(mediaItems);
		}
	}
}