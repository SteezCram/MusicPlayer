using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MusicPlayer.Views
{
    internal class Music
    {
        #region Public Members

        public string Author { get; set; }
        public string Title { get; set; }
        public int Duration { get; set; }
        public List<byte> Thumbnail { get; set; }
        public List<Note> Notes { get; set; }

        #endregion

        #region Static Methods

        private static JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

    /// <summary>
    /// Reads the music files from working directory.
    /// </summary>
    /// 
    /// <returns>An observable collection of musics</returns>
    public static ObservableCollection<Music> ReadMusic()
        {
            ObservableCollection<Music> musics = new ObservableCollection<Music>();

            Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Musics")).ToList().ForEach(x =>
            {
                Music music = JsonSerializer.Deserialize<Music>(File.ReadAllText(x), _serializerOptions);

                if (music.Duration == 0) {
                    music.Notes.ForEach(y => music.Duration += y.Duration);
                    File.WriteAllText(x, JsonSerializer.Serialize<Music>(music, _serializerOptions));
                }

                musics.Add(music);
            });

            return musics;
        }

        #endregion
    }

    internal struct Note
    {
        #region Public Members

        public int Duration { get; set; }
        public int Frequency { get; set; }

        #endregion
    }
}
