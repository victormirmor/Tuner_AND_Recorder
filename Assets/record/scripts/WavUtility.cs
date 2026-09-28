using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static void Save(string filePath, AudioClip clip)
    {
        var bytes = EncodeToWav(clip);
        File.WriteAllBytes(filePath, bytes);
    }

    private static byte[] EncodeToWav(AudioClip clip)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                int sampleRate = clip.frequency;
                int channels = clip.channels;
                int samplesCount = samples.Length;

                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + samplesCount * 2);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);

                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(samplesCount * 2);

                foreach (var sample in samples)
                {
                    short value = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767);
                    writer.Write(value);
                }
            }
            return memoryStream.ToArray();
        }
    }
}