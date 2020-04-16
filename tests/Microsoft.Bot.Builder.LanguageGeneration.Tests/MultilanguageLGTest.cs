using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.LanguageGeneration.Tests
{
    [TestClass]
    public class MultilanguageLGTest
    {
        [TestMethod]
        public void EmptyFallbackLocale()
        {
            var localPerFile = new Dictionary<string, string>
            {
                { "en", Path.Combine(AppContext.BaseDirectory, "MultiLanguage", "a.en.lg") },
                { string.Empty, Path.Combine(AppContext.BaseDirectory, "MultiLanguage", "a.lg") } // default local
            };

            var generator = new MultiLanguageLG(localPerFile);

            // fallback to "c.en.lg"
            var result = generator.Generate("templatec", null, "en-us");
            Assert.AreEqual("from a.en.lg", result);

            // "c.en.lg" is used
            result = generator.Generate("templatec", null, "en");
            Assert.AreEqual("from a.en.lg", result);

            // locale "fr" has no entry file, default file "c.lg" is used
            result = generator.Generate("templatec", null, "fr");
            Assert.AreEqual("from a.lg", result);

            // "c.lg" is used
            result = generator.Generate("templatec", null, null);
            Assert.AreEqual("from a.lg", result);
        }

        [TestMethod]
        public void SpecificFallbackLocale()
        {
            var localPerFile = new Dictionary<string, string>
            {
                { "en", Path.Combine(AppContext.BaseDirectory, "MultiLanguage", "a.en.lg") },
            };

            var generator = new MultiLanguageLG(localPerFile, "en");

            // fallback to "c.en.lg"
            var result = generator.Generate("templatec", null, "en-us");
            Assert.AreEqual("from a.en.lg", result);

            // "c.en.lg" is used
            result = generator.Generate("templatec", null, "en");
            Assert.AreEqual("from a.en.lg", result);

            // locale "fr" has no entry file, default file "c.en.lg" is used
            result = generator.Generate("templatec", null, "fr");
            Assert.AreEqual("from a.en.lg", result);

            // "c.en.lg" is used
            result = generator.Generate("templatec", null, null);
            Assert.AreEqual("from a.en.lg", result);
        }

        [TestMethod]
        public void TemplatesInputs()
        {
            var enTemplates = Templates.ParseText("[import](1.lg)\r\n # t\r\n - hi", "abc", AssemblyResolver);
            var deTemplates = Templates.ParseText("# template\r\n - de result.");

            var templatesDict = new Dictionary<string, Templates>
            {
                { "en", enTemplates },
                { "de", deTemplates },
            };

            var generator = new MultiLanguageLG(templatesDict, "de");

            // fallback to locale "en" 
            var result = generator.Generate("template", null, "en-us");
            Assert.AreEqual("this is a template from dll.", result);

            // locale "en" is used
            result = generator.Generate("template", null, "en");
            Assert.AreEqual("this is a template from dll.", result);

            // locale "fr" has no entry file, locale "de" is used
            result = generator.Generate("template", null, "fr");
            Assert.AreEqual("de result.", result);

            // default locale "en"
            result = generator.Generate("template", null, null);
            Assert.AreEqual("de result.", result);
        }

        private static (string content, string id) AssemblyResolver(string sourceId, string resourceId)
        {
            // assembly content:
            // # template
            // - this is a template from dll.

            var content = string.Empty;
            var assemblyPath = Path.Combine(AppContext.BaseDirectory, "ConsoleApp1.dll");
            var assembly = Assembly.LoadFile(assemblyPath);
            using (var sr = new StreamReader(assembly.GetManifestResourceStream("ConsoleApp1.1.lg")))
            {
                content = sr.ReadToEnd();
            }

            return (content, sourceId + resourceId);
        }
    }
}
