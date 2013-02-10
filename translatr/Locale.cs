using System;
using System.IO;

namespace translatr
{
    public enum LocaleID
    {
        Default = -1,
        English = 0,
        French,
        German,
        Italian,
        Spanish,
        Japanese,
        Portugese,
        Polish,
        EnglishUK,
        Russian,
        Czech,
        Dutch,
        Hungarian
    };

    class Locale
    {
        static public uint getLocaleMask(string dir)
        {
            uint mask = uint.MaxValue;

            // Get all directories containing locals.bin
            var dirs = Directory.GetFiles(dir, "locals.bin", SearchOption.AllDirectories);

            if (dirs.Length > 1)
            {
                foreach (string d in dirs)
                {
                    var locale = d.Substring(dir.Length + 1, 8);

                    if (locale == "default\\")
                    {
                        // We shouldnt have default!
                        throw new NotSupportedException();
                    }
                    else
                    {
                        mask &= uint.Parse(locale, System.Globalization.NumberStyles.HexNumber);
                    }
                }
                mask = ~mask;
            }
            else if (dirs.Length == 1)
            {
                var locale = dirs[0].Substring(dir.Length + 1, 8);
                if (locale == "default\\")
                    mask = uint.MaxValue - 1;
                else
                    mask = uint.MaxValue;
            }
            else
            {
                mask = uint.MaxValue;
            }

            return mask;
        }

        public static string toString(LocaleID id)
        {
            switch (id)
            {
                case LocaleID.English:
                    return "English";


                case LocaleID.French:
                    return "French";


                case LocaleID.German:
                    return "German";


                case LocaleID.Italian:
                    return "Italian";


                case LocaleID.Spanish:
                    return "Spanish";


                case LocaleID.Japanese:
                    return "Japanese";


                case LocaleID.Portugese:
                    return "Portugese";


                case LocaleID.Polish:
                    return "Polish";


                case LocaleID.EnglishUK:
                    return "EnglishUK";


                case LocaleID.Russian:
                    return "Russian";


                case LocaleID.Czech:
                    return "Czech";


                case LocaleID.Dutch:
                    return "Dutch";

                case LocaleID.Hungarian:
                    return "Hungarian";

                default:
                    return "Default";
            }
        }

        public static LocaleID getFromString(String s)
        {
            LocaleID id = LocaleID.Default;

            s = s.ToLower();

            switch (s)
            {
                case "english":
                case "en":
                    id = LocaleID.English;
                    break;

                case "french":
                case "fr":
                    id = LocaleID.French;
                    break;

                case "german":
                case "de":
                    id = LocaleID.German;
                    break;

                case "italian":
                case "it":
                    id = LocaleID.Italian;
                    break;

                case "spanish":
                case "es":
                    id = LocaleID.Spanish;
                    break;

                case "japanese":
                case "ja":
                    id = LocaleID.Japanese;
                    break;

                case "portugese":
                case "pt":
                    id = LocaleID.Portugese;
                    break;

                case "polish":
                case "pl":
                    id = LocaleID.Polish;
                    break;

                case "englishUK":
                case "uk":
                    id = LocaleID.EnglishUK;
                    break;

                case "russian":
                case "ru":
                    id = LocaleID.Russian;
                    break;

                case "czech":
                case "cs":
                    id = LocaleID.Czech;
                    break;

                case "dutch":
                case "nl":
                    id = LocaleID.Dutch;
                    break;

                case "hungarian":
                case "hu":
                    id = LocaleID.Hungarian;
                    break;
            }

            return id;
        }
    }
}
