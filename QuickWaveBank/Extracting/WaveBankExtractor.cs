/*******************************************************************************
 * Copyright (C) 2014-2015 Anton Gustafsson
 *
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 ******************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using QuickWaveBank.Util;

namespace QuickWaveBank.Extracting {
	public static class WaveBankExtractor {
		// XWB parsing was adapted from MonoGame

		// Track codecs
		private const int MiniFormatTag_PCM = 0x0;
		private const int MiniFormatTag_XMA = 0x1;
		private const int MiniFormatTag_ADPCM = 0x2;
		private const int MiniFormatTag_WMA = 0x3;

		private const int Flag_Compact = 0x00020000;

		// WAV Encoding
		private static readonly byte[] Label_RIFF = Encoding.UTF8.GetBytes("RIFF");
		private static readonly byte[] Label_WAVE = Encoding.UTF8.GetBytes("WAVE");
		private static readonly byte[] Label_XWMA = Encoding.UTF8.GetBytes("XWMA");
		// Note the space after fmt.
		private static readonly byte[] Label_fmt = Encoding.UTF8.GetBytes("fmt ");
		private static readonly byte[] Label_dpds = Encoding.UTF8.GetBytes("dpds");
		private static readonly byte[] Label_data = Encoding.UTF8.GetBytes("data");
		private static readonly int WavHeaderSize =
			Label_RIFF.Length + 4 + Label_WAVE.Length + Label_fmt.Length +
			4 + 2 + 2 + 4 + 4 + 2 + 2 + Label_data.Length + 4;

		private const string WaveBankList = "QuickWaveBank_TrackNames.txt";

		/** Mapping of music wave bank indexes to their names */
		public static string[] TrackNames = {
			"01 Overworld Night",
			"02 Eerie",
			"03 Overworld Day",
			"04 Boss 1",
			"05 Title Classic",
			"06 Jungle Day",
			"07 Corruption",
			"08 Hallow",
			"09 Underground Corruption",
			"10 Underground Hallow",
			"11 Boss 2",
			"12 Underground",
			"13 Boss 3",
			"14 Snow",
			"15 Space Night",
			"16 Crimson",
			"17 Boss 4",
			"18 Alt Overworld Day",
			"19 Rainy Day",
			"20 Underground Snow",
			"21 Desert",
			"22 Ocean Day",
			"23 Dungeon",
			"24 Plantera",
			"25 Boss 5",
			"26 Temple",
			"27 Eclipse",
			"28 Rain sound effect",
			"29 Mushrooms",
			"30 Pumpkin Moon",
			"31 Alt Underground",
			"32 Frost Moon",
			"33 Underground Crimson",
			"34 Lunar Event",
			"35 Pirate Invasion",
			"36 Hell",
			"37 Martian Madness",
			"38 Moon Lord",
			"39 Goblin Invasion",
			"40 Sandstorm",
			"41 Old Ones Army",
			"42 Space Day",
			"43 Ocean Night",
			"44 Windy Day",
			"45 Wind sound effect",
			"46 Town Day",
			"47 Town Night",
			"48 Slime Rain",
			"49 Overworld Day Remix",
			"50 Title Intro Journeys End",
			"51 Title Journeys End",
			"52 Storm",
			"53 Graveyard",
			"54 Underground Jungle",
			"55 Jungle Night",
			"56 Queen Slime",
			"57 Empress of Light",
			"58 Duke Fishron",
			"59 Morning Rain",
			"60 Title Console",
			"61 Underground Desert",
			"62 Otherworld Rain",
			"63 Otherworld Overworld Day",
			"64 Otherworld Overworld Night",
			"65 Otherworld Underground",
			"66 Otherworld Desert",
			"67 Otherworld Ocean",
			"68 Otherworld Mushrooms",
			"69 Otherworld Dungeon",
			"70 Otherworld Space",
			"71 Otherworld Hell",
			"72 Otherworld Snow",
			"73 Otherworld Corruption",
			"74 Otherworld Underground Corruption",
			"75 Otherworld Crimson",
			"76 Otherworld Underground Crimson",
			"77 Otherworld Underground Snow",
			"78 Otherworld Underground Hallow",
			"79 Otherworld Eerie",
			"80 Otherworld Boss 2",
			"81 Otherworld Boss 1",
			"82 Otherworld Invasion",
			"83 Otherworld Lunar Event",
			"84 Otherworld Moon Lord",
			"85 Otherworld Plantera",
			"86 Otherworld Jungle",
			"87 Otherworld Wall of Flesh",
			"88 Otherworld Hallow"
		};

		public static string GetTrackName(int index) {
			if (index < TrackNames.Length) {
				string name = TrackNames[index];
				if (name.Length >= 3) {
					if (char.IsDigit(name[0]) && char.IsDigit(name[1]) && (name[2] == '_' || name[2] == ' '))
						name = name.Substring(3).Trim();
				}
				return name;
			}
			return "Unknown";
		}

		static WaveBankExtractor() {
			// Try to find updated names of wave bank songs.
			// This way even if TConvert is not maintained, the wavebank can be updated.
			string path = Path.Combine(
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
				WaveBankList
			);
			if (File.Exists(path)) {
				ReadWaveBankList(path);
				return;
			}
			path = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"My Games", "Terraria", WaveBankList
			);
			if (File.Exists(path)) {
				ReadWaveBankList(path);
				return;
			}
			if (!string.IsNullOrEmpty(TerrariaLocator.TerrariaContentDirectory)) {
				path = Path.Combine(
					Path.GetDirectoryName(TerrariaLocator.TerrariaContentDirectory),
					WaveBankList
				);
				if (File.Exists(path)) {
					ReadWaveBankList(path);
					return;
				}
			}
		}
		private static void ReadWaveBankList(string filepath) {
			StreamReader reader = new StreamReader(filepath);
			List<string> tracknames = new List<string>();
			do {
				string name = reader.ReadLine();
				if (name != string.Empty)
					tracknames.Add(name);
			} while (!reader.EndOfStream);
			reader.Close();
			TrackNames = tracknames.ToArray();
		}

		private static void Status(String status) {
		}

		private static void Percentage(float percentage) {
		}


		public static bool CompareBytes(byte[] a, byte[] b) {
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}
		public static bool CompareBytesToString(byte[] a, string s) {
			return CompareBytes(a, Encoding.ASCII.GetBytes(s));
		}
		/**
		 * @param inputFile The XWB file to extract
		 * @param outputDirectory The directory to put the extracted files inside
		 * @param statusReporter The status reporter to use for reporting which tracks that are currently extracted.
		 * 
		 * @throws XnbException If the XWB file was malformed
		 * @throws IOException If an I/O error occurs
		 */
		public static bool Extract(string inputFile, string outputDirectory) {
			Status("Parsing XWB file header");
			Percentage(0f);

			//ByteBuffer buffer = ByteBuffer.wrap(FileUtils.readFileToByteArray(inputFile));
			//buffer.order(ByteOrder.LITTLE_ENDIAN);
			BinaryReader reader = new BinaryReader(new FileStream(inputFile, FileMode.Open));

			int Format = 0;
			int PlayRegionLength = 0;
			int PlayRegionOffset = 0;

			int wavebank_offset = 0;

			if (!CompareBytesToString(reader.ReadBytes(4), "WBND")) {
				throw new Exception("not an XWB file: " + Path.GetFileName(inputFile));
			}

			int Version = reader.ReadInt32();

			// Skip trailing bytes of the version
			reader.ReadInt32();

			if (Version != 46) {
				throw new Exception("unsupported version: " + Version);
			}

			int[] segmentOffsets = new int[5];
			int[] segmentLengths = new int[5];

			for (int i = 0; i< 5; i++) {
				segmentOffsets[i] = reader.ReadInt32();
				segmentLengths[i] = reader.ReadInt32();
			}

			reader.BaseStream.Position = segmentOffsets[0];

			int Flags = reader.ReadInt32();
			int EntryCount = reader.ReadInt32();

			// Skip terraria's wave bank's name. "Wave Bank".
			reader.BaseStream.Position += 64;

			int EntryMetaDataElementSize = reader.ReadInt32();
			reader.ReadInt32(); // EntryNameElementSize
			reader.ReadInt32(); // Alignment
			wavebank_offset = segmentOffsets[1];

			if ((Flags & Flag_Compact) != 0) {
				throw new Exception("compact wavebanks are not supported");
			}

			int playregion_offset = segmentOffsets[4];
			for (int current_entry = 0; current_entry<EntryCount; current_entry++) {
				String track = current_entry < TrackNames.Length ? SanitizeFileName(TrackNames[current_entry], "_") : (current_entry + 1) + " Unknown";

				Status("Extracting " + track);
				Percentage(0.1f + (0.9f / EntryCount) * current_entry);

				reader.BaseStream.Position = wavebank_offset;
				if (EntryMetaDataElementSize >= 4)
					reader.ReadInt32(); // FlagsAndDuration
				if (EntryMetaDataElementSize >= 8)
					Format = reader.ReadInt32();
				if (EntryMetaDataElementSize >= 12)
					PlayRegionOffset = reader.ReadInt32();
				if (EntryMetaDataElementSize >= 16)
					PlayRegionLength = reader.ReadInt32();
				if (EntryMetaDataElementSize >= 20)
					reader.ReadInt32(); // LoopRegionOffset
				if (EntryMetaDataElementSize >= 24)
					reader.ReadInt32(); // LoopRegionLength

				wavebank_offset += EntryMetaDataElementSize;
				PlayRegionOffset += playregion_offset;

				int codec = (Format) & ((1 << 2) - 1);
				int chans = (Format >> (2)) & ((1 << 3) - 1);
				int rate = (Format >> (2 + 3)) & ((1 << 18) - 1);
				int align = (Format >> (2 + 3 + 18)) & ((1 << 8) - 1);

				reader.BaseStream.Position = PlayRegionOffset;
				byte[] audiodata = reader.ReadBytes(PlayRegionLength);

				// The codecs used by Terraria are currently xWMA and ADPCM.
				// The xWMA format is not supported by FNA, so it's only used
				// on Windows. This implementation uses ffmpeg to convert the raw
				// xWMA data to WAVE; a minified Windows executable is embedded.
				// PCM was introduced for the last tracks in the 1.3.3 update.
				string path = Path.Combine(outputDirectory, track + ".wav");
				if (codec == MiniFormatTag_PCM) {
					FileStream stream = new FileStream(path, FileMode.OpenOrCreate);
					BinaryWriter writer = new BinaryWriter(stream);
					stream.SetLength(0);
					writer.Write(Label_RIFF); // chunk id
					writer.Write(audiodata.Length + 36); // chunk size
					writer.Write(Label_WAVE); // RIFF type
					writer.Write(Label_fmt); // chunk id
					writer.Write((int)16); // format header size
					writer.Write((short)1); // format (PCM)
					writer.Write((short)chans); // channels
					writer.Write(rate); // samples per second
					int bitsPerSample = 16;
					int blockAlign = (bitsPerSample / 8) * chans;
					writer.Write(rate * blockAlign); // byte rate/ average bytes per second
					writer.Write((short)blockAlign);
					writer.Write((short)bitsPerSample);
					writer.Write(Label_data); // chunk id
					writer.Write(audiodata.Length); // data size


					writer.Write(audiodata);
					writer.Close();
				}
				else if (codec == MiniFormatTag_WMA) {
					// Note that it could still be another codec than xWma,
					// but that scenario isn't handled here.

					// This part has been ported from XWMA-to-pcm-u8
					// Not the most beautiful code in the world,
					// but it does the job.

					// I do not know if this code outputs valid XWMA files,
					// but FFMPEG accepts them so it's all right for this usage.

					//File xWmaFile = new File(outputDirectory, track + ".wma");

					//FileOutputStream xWmaOutput = FileUtils.openOutputStream(xWmaFile);
					// xWmaOutput.write(output.array(), output.arrayOffset(), output.position());
					string wmaPath = Path.Combine(outputDirectory, track + ".wma");
					BinaryWriter writer = new BinaryWriter(new FileStream(wmaPath, FileMode.OpenOrCreate));

					//BufferWriter output = new BufferWriter(xWmaOutput);
					//output.setOrder(ByteOrder.LITTLE_ENDIAN);
					writer.Write(Label_RIFF); // chunk id
					writer.Write(0); // Full file size, ignored by ffmpeg
					writer.Write(Label_XWMA); // RIFF type
					writer.Write(Label_fmt); // chunk id
					writer.Write((int)18); // format header size
					writer.Write((short)0x161); // format (PCM)
					writer.Write((short)chans); // channels
					writer.Write(rate); // samples per second


					int[] wmaAverageBytesPerSec = new int[] {
						12000, 24000, 4000, 6000, 8000, 20000
					};
					int[] wmaBlockAlign = new int[] {
						929, 1487, 1280, 2230, 8917, 8192, 4459, 5945,
						2304, 1536, 1485, 1008, 2731, 4096, 6827, 5462
					};

					int averageBytesPerSec = align > wmaAverageBytesPerSec.Length ? wmaAverageBytesPerSec[align >> 5] : wmaAverageBytesPerSec[align];

					int blockAlign = align > wmaBlockAlign.Length ? wmaBlockAlign[align & 0xf] : wmaBlockAlign[align];

					writer.Write(averageBytesPerSec);
					writer.Write((short)blockAlign);
					writer.Write((short)16);
					writer.Write((short)0);
					writer.Write(Label_dpds);
					int packetLength = blockAlign;
					int packetNum = audiodata.Length / packetLength;
					writer.Write(packetNum * 4);

					int fullSize = (PlayRegionLength * averageBytesPerSec % 4096 != 0) ? (1 + (int) (PlayRegionLength
							* averageBytesPerSec / 4096)) * 4096
							: PlayRegionLength;
					int allBlocks = fullSize / 4096;
					int avgBlocksPerPacket = allBlocks / packetNum;
					int spareBlocks = allBlocks - (avgBlocksPerPacket * packetNum);

					int accu = 0;
					for (int i = 0; i<packetNum; ++i) {
						accu += avgBlocksPerPacket* 4096;
						if (spareBlocks != 0) {
							accu += 4096;
							--spareBlocks;
						}
						writer.Write(accu);
					}

					writer.Write(Label_data);

					writer.Write(PlayRegionLength);
					writer.Write(audiodata);
					// Replacing the file size placeholder, dosen't matter with ffmpeg
					// int pos = output.position();
					// output.position(odRIChunkSize);
					// output.putInt(pos - 8);
					// output.position(pos);
					writer.Close();

					FFmpeg.Convert(wmaPath, path);

					File.Delete(wmaPath);
				}
				else if (codec == MiniFormatTag_ADPCM) {
					// Convert ADPCM data to PCM
					audiodata = ADPCMConverter.ConvertToPCM(audiodata, (short)chans, (short)align);
					// Encode PCM as a WAVE file; note that most magic values used
					// here were obtained via trial and error, so it might break...
					//ByteBuffer writeBuffer = ByteBuffer.allocate(wavHeaderSize);
					//writeBuffer.order(ByteOrder.LITTLE_ENDIAN);
					FileStream stream = new FileStream(path, FileMode.OpenOrCreate);
					BinaryWriter writer = new BinaryWriter(stream);
					stream.SetLength(0);
					writer.Write(Label_RIFF);
					writer.Write(audiodata.Length + 36);
					writer.Write(Label_WAVE);
					writer.Write(Label_fmt);
					writer.Write((int)16);
					writer.Write((short)1); // format code
					writer.Write((short)chans); // channels
					writer.Write(rate); // blocks per second
					writer.Write(rate * 4); // bytes per second
					writer.Write((short)4); // data block alignment
					writer.Write((short)16); // bits per sample
					writer.Write(Label_data);
					writer.Write(audiodata.Length); // dataChunkSize

					writer.Write(audiodata);
					writer.Close();
				}
				else {
					throw new Exception("unimplemented codec " + codec);
				}
			}
			reader.Close();
			return true;
		}
		
		// Safety methods for people who who may...
		//  A) Put very dangerous characters in their filenames
		//  B) Rename some random binary file to "QuickWaveBank_TrackNames.txt", (the horror!)
		private static string SanitizeFileName(string s, string replaceCharsWith) {
			StringBuilder sanitized = new StringBuilder();
			SanitizeFileName(sanitized, replaceCharsWith);
			return sanitized.ToString();
		}
		private static StringBuilder SanitizeFileName(StringBuilder sanitized, string replaceCharsWith) {
			// Sanitize invalid Filename/filepath characters
			foreach (char specialChar in Path.GetInvalidFileNameChars()) {
				sanitized.Replace(new string(specialChar, 1), replaceCharsWith);
			}
			// We don't want duplicate spaces...?
			sanitized.Replace("  ", " ");

			// Avoid environment variable evaluation (Windows)
			sanitized.Replace("%", replaceCharsWith);

			// Huh... no StringBuilder.Trim()
			// Trim beginning and trailing ' ','.'
			while (sanitized.Length > 0) {// TrimStart
				char c = sanitized[0];
				if (c != ' ' && c != '.')
					break;
				sanitized.Remove(0, 1);
			}
			while (sanitized.Length > 0) { // TrimEnd
				char c = sanitized[sanitized.Length - 1];
				if (c != ' ' && c != '.')
					break;
				sanitized.Remove(sanitized.Length - 1, 1);
			}
			
			// Handle remaining control characters
			for (int i = 0; i < sanitized.Length; i++) {
				char c = sanitized[i];
				// Control characters. NO!
				if (c < (char)0x20 || c == (char) 0x7f) {
					sanitized.Remove(i, 1);
					sanitized.Insert(i, replaceCharsWith);
					i += replaceCharsWith.Length - 1;  // -1 for removed [i]
				}
			}

			return sanitized;
		}
	}
}
