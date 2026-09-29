using Godot;

public static class Sfx
{
	public static AudioStreamWav Kick => Tone(180, 0.09f, 0.7f);
	public static AudioStreamWav Goal => Chord(0.28f, 0.55f, 523, 659, 784);
	public static AudioStreamWav Save => Tone(90, 0.16f, 0.65f);
	public static AudioStreamWav Miss => Tone(220, 0.18f, 0.4f, fall: true);
	public static AudioStreamWav Crowd => Noise(1.4f, 0.12f);

	private static AudioStreamWav Tone(float hz, float seconds, float volume, bool fall = false)
	{
		int rate = 22050;
		int n = Mathf.Max(2, (int)(rate * seconds));
		var data = new byte[n * 2];
		for (int i = 0; i < n; i++)
		{
			float t = i / (float)n;
			float env = t < 0.1f ? t / 0.1f : 1f - (t - 0.1f) / 0.9f;
			float freq = fall ? hz * (1f - 0.5f * t) : hz;
			short sample = (short)(Mathf.Sin(t * seconds * freq * Mathf.Tau) * env * volume * short.MaxValue);
			data[i * 2] = (byte)(sample & 0xff);
			data[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
		}

		return Stream(data, rate);
	}

	private static AudioStreamWav Chord(float seconds, float volume, params float[] hz)
	{
		int rate = 22050;
		int n = Mathf.Max(2, (int)(rate * seconds));
		var data = new byte[n * 2];
		for (int i = 0; i < n; i++)
		{
			float t = i / (float)n;
			float env = t < 0.08f ? t / 0.08f : 1f - (t - 0.08f) / 0.92f;
			float mix = 0f;
			foreach (float f in hz)
				mix += Mathf.Sin(t * seconds * f * Mathf.Tau);
			short sample = (short)(mix / hz.Length * env * volume * short.MaxValue);
			data[i * 2] = (byte)(sample & 0xff);
			data[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
		}

		return Stream(data, rate);
	}

	private static AudioStreamWav Noise(float seconds, float volume)
	{
		int rate = 22050;
		int n = Mathf.Max(2, (int)(rate * seconds));
		var data = new byte[n * 2];
		uint seed = 1234567;
		for (int i = 0; i < n; i++)
		{
			seed ^= seed << 13;
			seed ^= seed >> 17;
			seed ^= seed << 5;
			float nse = (seed / (float)uint.MaxValue) * 2f - 1f;
			short sample = (short)(nse * volume * short.MaxValue);
			data[i * 2] = (byte)(sample & 0xff);
			data[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
		}

		return Stream(data, rate, loop: true);
	}

	private static AudioStreamWav Stream(byte[] data, int rate, bool loop = false)
	{
		var stream = new AudioStreamWav
		{
			Format = AudioStreamWav.FormatEnum.Format16Bits,
			MixRate = rate,
			Stereo = false,
			Data = data,
		};
		if (loop)
		{
			stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
			stream.LoopEnd = data.Length / 2;
		}

		return stream;
	}
}
