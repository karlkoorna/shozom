using System.Text.Json.Serialization;

namespace Shozom {

	internal class ShozomMatch {

		public string Title { get; set; }

		public string Artist { get; set; }

		public string Link { get; set; }

		public string Cover { get; set; }

	}

	#region Shazam Request

	internal class ShazamRequest {

		[JsonPropertyName("signature")]
		public ShazamSignature Signature { get; set; }

	}

	internal class ShazamSignature {

		[JsonPropertyName("uri")]
		public string Uri { get; set; }

		[JsonPropertyName("samplems")]
		public int SampleMs { get; set; }

	}

	#endregion

	#region Shazam Response

	internal class ShazamResponse {

		[JsonPropertyName("track")]
		public ShazamTrack Track { get; set; }

		[JsonPropertyName("retryms")]
		public int? RetryMs { get; set; }

	}

	internal class ShazamTrack {

		[JsonPropertyName("title")]
		public string Title { get; set; }

		[JsonPropertyName("subtitle")]
		public string Subtitle { get; set; }

		[JsonPropertyName("images")]
		public ShazamImages Images { get; set; }

		[JsonPropertyName("share")]
		public ShazamShare Share { get; set; }

	}

	internal class ShazamImages {

		[JsonPropertyName("coverart")]
		public string Cover { get; set; }

		[JsonPropertyName("coverarthq")]
		public string CoverHQ { get; set; }

	}

	internal class ShazamShare {

		[JsonPropertyName("href")]
		public string Link { get; set; }

		[JsonPropertyName("image")]
		public string Image { get; set; }

	}

	#endregion

}
