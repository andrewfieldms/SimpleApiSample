namespace SampleApiSample
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Google.Apis.YouTube.v3;
	using Newtonsoft.Json;

	public class SubSplashResponse
	{
		public class embedded
		{
			[JsonProperty(PropertyName = "media-items")]
			public mediaItem[] @mediaitems;
		}
		public class mediaItem
		{
			public string id;
			public string title;
			public string youtube_url;
			public int reach;
			public string published_at;
		}

		public int count;
		public int total;
		public embedded _embedded;

		public string CreateSampleData()
		{
			var youTube = Program.getYouTubeService();
			var search = youTube.Videos.List("snippet"); //.Search.List("snippet,contentDetails");
			search.Chart = VideosResource.ListRequest.ChartEnum.MostPopular;
			search.MaxResults = 40;

			var result = search.Execute();

			Random rand = new Random();
			List<SubSplashResponse.mediaItem> mediaItems = new List<SubSplashResponse.mediaItem>();

			foreach (var i in result.Items.ToList())
			{
				mediaItems.Add(new SubSplashResponse.mediaItem()
				{
					id = i.Id,
					title = i.Snippet.Title,
					youtube_url = "https://www.youtube.com/watch?v=" + i.Id,
					published_at = DateTime.Today.AddDays(-rand.Next(90)).ToString(),
					reach = rand.Next(1000000)
				});
			}

			return JsonConvert.SerializeObject(mediaItems);
		}
	}
}