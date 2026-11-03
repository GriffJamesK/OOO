using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Kit.Tests.EditMode
{
    /// <summary>
    /// Keeps docs/references/ and its REFERENCES.md in step. Agents only know what to take from (and
    /// what to ignore in) an image through the owner's notes, so every image needs a section and
    /// every section must point at an image that is really there.
    /// </summary>
    public class ReferenceNotesTests
    {
        static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

        static string ReferencesDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "references"));
        static string NotesPath => Path.Combine(ReferencesDir, "REFERENCES.md");

        /// <summary>Image paths relative to docs/references/, with forward slashes.</summary>
        static List<string> Images()
        {
            return Directory.GetFiles(ReferencesDir, "*", SearchOption.AllDirectories)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(f => f.Substring(ReferencesDir.Length + 1).Replace('\\', '/'))
                .ToList();
        }

        /// <summary>Every "## name" heading outside HTML comments, except General (which is for all images).</summary>
        static List<string> NoteSections()
        {
            string text = Regex.Replace(File.ReadAllText(NotesPath), "<!--.*?-->", "", RegexOptions.Singleline);
            return Regex.Matches(text, @"^##\s+(.+?)\s*$", RegexOptions.Multiline)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value.Trim('`', ' '))
                .Where(h => h != "General")
                .ToList();
        }

        [Test]
        public void ReferencesFolder_HasNotesFile()
        {
            Assert.IsTrue(File.Exists(NotesPath), $"Missing {NotesPath}: agents read the owner's reference notes from it.");
        }

        [Test]
        public void EveryReferenceImage_HasANotesSection()
        {
            var sections = new HashSet<string>(NoteSections(), System.StringComparer.OrdinalIgnoreCase);
            var missing = Images().Where(i => !sections.Contains(i)).ToList();
            Assert.IsEmpty(missing, "Add a '## <file name>' section to docs/references/REFERENCES.md for: " + string.Join(", ", missing));
        }

        [Test]
        public void EveryNotesSection_NamesAnImageThatExists()
        {
            var images = new HashSet<string>(Images(), System.StringComparer.OrdinalIgnoreCase);
            var orphans = NoteSections().Where(s => !images.Contains(s)).ToList();
            Assert.IsEmpty(orphans, "docs/references/REFERENCES.md has notes for images that aren't in the folder: " + string.Join(", ", orphans));
        }
    }
}
