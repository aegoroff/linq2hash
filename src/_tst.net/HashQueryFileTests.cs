﻿/*
 * Created by: egr
 * Created at: 05.12.2011
 * © 2009-2013 Alexander Egorov
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace _tst.net
{
    public abstract class HashQueryFileTests<THash> : HashBase<THash> where THash : Hash, new()
    {
        private const string EmptyFileName = "e_mpty";
        private const string NotEmptyFileName = "n_otempty";
        private const string Slash = @"\";
        private const string BaseTestDir = @"C:\_tst.net";
        private const string NotEmptyFile = BaseTestDir + Slash + NotEmptyFileName;
        private const string EmptyFile = BaseTestDir + Slash + EmptyFileName;
        private const string SubDir = BaseTestDir + Slash + "sub";
        private const string QueryOpt = "-C";
        private const string FileOpt = "-F";
        private const string ParamOpt = "-P";
        private const string TimeOpt = "-t";
        private const string NoProbeOpt = "--noprobe";
        private const string HashStringQueryTpl = "for string '{0}' do {1};";
        
        
        private const string FileResultTpl = @"{0} | {2} bytes | {1}";
        private const string FileResultTimeTpl = @"^(.*?) | \d bytes | \d\.\d{3} sec | ([0-9a-zA-Z]{32,128}?)$";
        private const string FileSearchTpl = @"{0} | {1} bytes";
        private const string FileSearchTimeTpl = @"^(.*?) | \d bytes$";
        
        private const string QueryFile = BaseTestDir + Slash + "hl.hlq";
        private const string ValidationQueryTemplate = "for file f from '{0}' let f.{1} = '{2}' do validate;";
        private const string SearchFileQueryTemplate = "for file f from dir '{0}' where f.{1} == '{2}' do find;";
        private const string CalculateFileQueryTemplate = "for file f from '{0}' do {1};";
        

        protected override string EmptyFileNameProp
        {
            get { return EmptyFileName; }
        }

        protected override string EmptyFileProp
        {
            get { return EmptyFile; }
        }

        protected override string NotEmptyFileNameProp
        {
            get { return NotEmptyFileName; }
        }

        protected override string NotEmptyFileProp
        {
            get { return NotEmptyFile; }
        }

        protected override string BaseTestDirProp
        {
            get { return BaseTestDir; }
        }

        protected override string SubDirProp
        {
            get { return SubDir; }
        }

        protected override string SlashProp
        {
            get { return Slash; }
        }

        protected override string Executable
        {
            get { return "hc.exe"; }
        }

        IList<string> RunQuery(string template, params object[] parameters)
        {
            return Runner.Run(QueryOpt, string.Format(template, parameters), NoProbeOpt);
        }
        
        IList<string> RunValidatingQuery(string path, string template, params object[] parameters)
        {
            return Runner.Run(QueryOpt, string.Format(template, parameters), ParamOpt, path);
        }
        
        IList<string> RunFileQuery(string template, params object[] parameters)
        {
            File.WriteAllText(QueryFile, string.Format(template, parameters));
            return Runner.Run(FileOpt, QueryFile);
        }
        
        IList<string> RunQueryWithOpt(string template, string additionalOptions, params object[] parameters)
        {
            return Runner.Run(QueryOpt, string.Format(template, parameters), additionalOptions);
        }

        [TearDown]
        public void Teardown()
        {
            if (File.Exists(QueryFile))
            {
                File.Delete(QueryFile);
            }
        }

        [Test]
        public void FileQuery()
        {
            IList<string> results = RunFileQuery(HashStringQueryTpl, InitialString, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(HashString));
        }
        
        [Test]
        public void FileWithSeveralQueries()
        {
            const int count = 2;
            IList<string> results = RunFileQuery(SeveralQueries(count), InitialString, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(count));
            Assert.That(results[0], Is.EqualTo(HashString));
            Assert.That(results[1], Is.EqualTo(HashString));
        }

        private static string SeveralQueries( int count )
        {
            return string.Join(Environment.NewLine, CreateSeveralStringQueries(count));
        }

        static IEnumerable<string> CreateSeveralStringQueries(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return HashStringQueryTpl;
            }
        }

        [Test]
        public void CalcFile()
        {
            IList<string> results = RunQuery(CalculateFileQueryTemplate, NotEmptyFile, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0],
                        Is.EqualTo(string.Format(FileResultTpl, NotEmptyFile, HashString, InitialString.Length)));
        }

        [Test]
        public void CalcFileTime()
        {
            IList<string> results = RunQueryWithOpt(CalculateFileQueryTemplate, TimeOpt, NotEmptyFile, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0], Is.StringMatching(FileResultTimeTpl));
        }

        [TestCase(1, "File is valid")]
        [TestCase(0, "File is invalid")]
        public void ValidateParameterFile(int offset, string result)
        {
            IList<string> results = RunValidatingQuery(NotEmptyFile, "for file f from parameter where f.offset == {0} and f.{1} == '{2}' do validate;", offset, Hash.Algorithm, TrailPartStringHash);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0],
                        Is.EqualTo(string.Format(FileResultTpl, NotEmptyFile, result, InitialString.Length)));
        }

        [Test]
        public void CalcFileLimit()
        {
            IList<string> results = RunQuery("for file f from '{0}' let f.limit = {1} do {2};", NotEmptyFile, 2, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0],
                        Is.EqualTo(string.Format(FileResultTpl, NotEmptyFile, StartPartStringHash, InitialString.Length)));
        }

        [Test]
        public void CalcFileOffset()
        {
            IList<string> results = RunQuery("for file f from '{0}' let f.offset = {1} do {2};", NotEmptyFile, 1, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0],
                        Is.EqualTo(string.Format(FileResultTpl, NotEmptyFile, TrailPartStringHash, InitialString.Length)));
        }

        [Test]
        public void CalcFileLimitAndOffset()
        {
            IList<string> results = RunQuery("for file f from '{0}' let f.limit = {1}, f.offset = {1} do {2};", NotEmptyFile, 1, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0],
                        Is.EqualTo(string.Format(FileResultTpl, NotEmptyFile, MiddlePartStringHash, InitialString.Length)));
        }

        [Test]
        public void CalcFileOffsetGreaterThenFileSIze()
        {
            IList<string> results = RunQuery("for file f from '{0}' let f.offset = {1} do {2};", NotEmptyFile, 4, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0],
                        Is.EqualTo(string.Format(FileResultTpl, NotEmptyFile, "Offset is greater then file size",
                                                 InitialString.Length)));
        }

        [Test]
        public void CalcBigFile()
        {
            const string file = NotEmptyFile + "_big";
            CreateNotEmptyFile(file, 2 * 1024 * 1024);
            try
            {
                IList<string> results = RunQuery(CalculateFileQueryTemplate, file, Hash.Algorithm);
                Assert.That(results.Count, Is.EqualTo(1));
                StringAssert.Contains(" Mb (2", results[0]);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void CalcBigFileWithOffset()
        {
            const string file = NotEmptyFile + "_big";
            CreateNotEmptyFile(file, 2 * 1024 * 1024);
            try
            {
                IList<string> results = RunQuery("for file f from '{0}' let f.offset = {1} do {2};", file, 1024, Hash.Algorithm);
                Assert.That(results.Count, Is.EqualTo(1));
                StringAssert.Contains(" Mb (2", results[0]);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void CalcBigFileWithLimitAndOffset()
        {
            const string file = NotEmptyFile + "_big";
            CreateNotEmptyFile(file, 2 * 1024 * 1024);
            try
            {
                IList<string> results = RunQuery("for file f from '{0}' let f.limit = {1}, f.offset = {2} do {3};", file, 1048500, 1024, Hash.Algorithm);
                Assert.That(results.Count, Is.EqualTo(1));
                StringAssert.Contains(" Mb (2", results[0]);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void CalcUnexistFile()
        {
            const string unexist = "u";
            IList<string> results = RunQuery(CalculateFileQueryTemplate, unexist, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(1));
            string en = string.Format("{0} | The system cannot find the file specified.  ", unexist);
            string ru = string.Format("{0} | Не удается найти указанный файл.  ", unexist);
            Assert.That(results[0], Is.InRange(en, ru));
        }

        [Test]
        public void CalcEmptyFile()
        {
            IList<string> results = RunQuery(CalculateFileQueryTemplate, EmptyFile, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(string.Format(FileResultTpl, EmptyFile, EmptyStringHash, 0)));
        }

        [Test]
        public void CalcDir()
        {
            IList<string> results = RunQuery("for file f from dir '{0}' do {1};", BaseTestDir, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0], Is.EqualTo(string.Format(FileResultTpl, EmptyFile, EmptyStringHash, 0)));
            Assert.That(results[1],
                        Is.EqualTo(string.Format(FileResultTpl, NotEmptyFile, HashString, InitialString.Length)));
        }

        [Test]
        public void CalcDirRecursivelyManySubs()
        {
            const string sub2Suffix = "2";
            Directory.CreateDirectory(SubDir + sub2Suffix);

            CreateEmptyFile(SubDir + sub2Suffix + Slash + EmptyFileName);
            CreateNotEmptyFile(SubDir + sub2Suffix + Slash + NotEmptyFileName);

            try
            {
                IList<string> results = RunQuery("for file f from dir '{0}' do {1} withsubs;", BaseTestDir, Hash.Algorithm);
                Assert.That(results.Count, Is.EqualTo(6));
            }
            finally
            {
                Directory.Delete(SubDir + sub2Suffix, true);
            }
        }

        [Test]
        public void CalcDirIncludeFilter()
        {
            IList<string> results = RunQuery("for file f from dir '{0}' where f.name ~ '{1}' do {2};", BaseTestDir, EmptyFileName, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(string.Format(FileResultTpl, EmptyFile, EmptyStringHash, 0)));
        }
        
        [Test]
        public void CalcDirIncludeFilterWithVar()
        {
            IList<string> results = RunQuery("let x = '{0}';let y = '{1}';for file f from dir x where f.name ~ y do {2};", BaseTestDir, EmptyFileName, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(string.Format(FileResultTpl, EmptyFile, EmptyStringHash, 0)));
        }

        [Test]
        public void CalcDirExcludeFilter()
        {
            IList<string> results = RunQuery("for file f from dir '{0}' where f.name !~ '{1}' do {2};", BaseTestDir, EmptyFileName, Hash.Algorithm);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0],
                        Is.EqualTo(string.Format(FileResultTpl, NotEmptyFile, HashString, InitialString.Length)));
        }

        [TestCase(0, "for file f from dir '{0}' where f.name ~ '{1}' and f.name !~ '{1}' do {2};", BaseTestDir, EmptyFileName)]
        [TestCase(0, "for file f from dir '{0}' where f.name !~ '{1}' and f.name !~ '{2}' do {3};", BaseTestDir, EmptyFileName, NotEmptyFileName)]
        [TestCase(2, "for file f from dir '{0}' where f.name ~ '{1}' or f.name ~ '{2}' do {3};", BaseTestDir, EmptyFileName, NotEmptyFileName)]
        [TestCase(2, "for file f from dir '{0}' where f.name ~ '{1}' do {2} withsubs;", BaseTestDir, EmptyFileName)]
        [TestCase(2, "for file f from dir '{0}' where f.name !~ '{1}' do {2} withsubs;", BaseTestDir, EmptyFileName)]
        [TestCase(4, "for file f from dir '{0}' do {1} withsubs;", new object[] { BaseTestDir })]
        [TestCase(2, "for file f from dir '{0}' where f.size == 0 do {1} withsubs;", new object[] { BaseTestDir })]
        [TestCase(1, "for file f from dir '{0}' where f.size == 0 do {1};", new object[] { BaseTestDir })]
        [TestCase(2, "for file f from dir '{0}' where f.size != 0 do {1} withsubs;", new object[] { BaseTestDir })]
        [TestCase(1, "for file f from dir '{0}' where f.size != 0 do {1};", new object[] { BaseTestDir })]
        [TestCase(4, "for file f from dir '{0}' where f.size == 0 or f.name ~ '{1}' do {2} withsubs;", BaseTestDir, NotEmptyFileName)]
        [TestCase(2, "for file f from dir '{0}' where f.size == 0 or not f.name ~ '{1}' do {2} withsubs;", BaseTestDir, NotEmptyFileName)]
        [TestCase(0, "for file f from dir '{0}' where f.size == 0 and f.name ~ '{1}' do {2} withsubs;", BaseTestDir, NotEmptyFileName)]
        [TestCase(4, "for file f from dir '{0}' where f.name ~ '{1}' or f.name ~ '{2}' do {3} withsubs;", BaseTestDir, EmptyFileName, NotEmptyFileName)]
        [TestCase(2, "for file f from dir '{0}' where f.name ~ '{1}' or (f.name ~ '{2}' and f.size == 0) do {3} withsubs;", BaseTestDir, EmptyFileName, NotEmptyFileName)]
        [TestCase(3, "for file f from dir '{0}' where (f.name ~ '{1}' and (f.name ~ '{2}' or f.size == 0)) or f.path ~ '{3}' do {4} withsubs;", BaseTestDir, EmptyFileName, NotEmptyFileName, @".*sub.*")]
        [TestCase(1, "for file f from dir '{0}' where (f.name ~ '{1}' and (f.name ~ '{2}' or f.size == 0)) and f.path ~ '{3}' do {4} withsubs;", BaseTestDir, EmptyFileName, NotEmptyFileName, @".*sub.*")]
        [TestCase(0, "for file f from dir '{0}' where f.name ~ '' do {1} withsubs;", new object[] { BaseTestDir })]
        [TestCase(4, "for file f from dir '{0}' where f.name !~ '' do {1} withsubs;", new object[] { BaseTestDir })]
        [TestCase(4, "for file f from dir '{0}' where f.name ~ 'mpty' do {1} withsubs;", new object[] { BaseTestDir })]
        public void CalcDir(int countResults, string template, params object[] parameters)
        {
            List<object> p = parameters.ToList();
            p.Add(Hash.Algorithm);
            IList<string> results = RunQuery(template, p.ToArray());
            Assert.That(results.Count, Is.EqualTo(countResults));
        }

        [Test]
        public void SearchFile()
        {
            IList<string> results = RunQuery(SearchFileQueryTemplate, BaseTestDir, Hash.Algorithm, HashString);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0],
                        Is.EqualTo(string.Format(FileSearchTpl, NotEmptyFile, InitialString.Length)));
        }
        
        [Test]
        public void SearshFileNotEq()
        {
            IList<string> results = RunQuery("for file f from dir '{0}' where f.{1} != '{2}' do find;", BaseTestDir, Hash.Algorithm, HashString);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0],
                        Is.EqualTo(string.Format(FileSearchTpl, EmptyFile, 0)));
        }

        [Test]
        public void SearchFileTimed()
        {
            IList<string> results = RunQueryWithOpt(SearchFileQueryTemplate, TimeOpt, BaseTestDir, Hash.Algorithm, HashString);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0], Is.StringMatching(FileSearchTimeTpl));
        }

        [Test]
        public void SearchFileRecursively()
        {
            IList<string> results = RunQuery("for file f from dir '{0}' where f.{1} == '{2}' do find withsubs;", BaseTestDir, Hash.Algorithm, HashString);
            Assert.That(results.Count, Is.EqualTo(2));
        }
        
        [Test]
        public void SearchFileOffsetMoreThenFileSize()
        {
            IList<string> results = RunQuery("for file f from dir '{0}' where f.offset == 100 and f.{1} == '{2}' do find withsubs;", BaseTestDir, Hash.Algorithm, HashString);
            Assert.That(results, Is.Empty);
        }
        
        [Test]
        public void SearchFileOffsetNegative()
        {
            IList<string> results = RunQuery("for file f from dir '{0}' where f.offset == -10 and f.{1} == '{2}' do find withsubs;", BaseTestDir, Hash.Algorithm, HashString);
            Assert.That(results, Is.Empty);
        }
        
        [Test]
        public void SearchFileOffsetOverflow()
        {
            IList<string> results = RunQuery("for file f from dir '{0}' where f.offset == 9223372036854775808 and f.{1} == '{2}' do find withsubs;", BaseTestDir, Hash.Algorithm, HashString);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void SearchFileSeveralHashes()
        {
            IList<string> results = RunQuery("for file f from dir '{0}' where f.offset == 1 and f.{1} == '{2}' and f.offset == 1 and f.limit == 1 and f.{1} == '{3}' do find;", BaseTestDir, Hash.Algorithm, TrailPartStringHash, MiddlePartStringHash);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0],
                        Is.EqualTo(string.Format(FileSearchTpl, NotEmptyFile, InitialString.Length)));
        }
        
        [Test]
        public void SearchFileSeveralHashesNoResults()
        {
            IList<string> results = RunQuery("for file f from dir '{0}' where f.offset == 1 and f.{1} == '{2}' and f.offset == 1 and f.limit == 1 and f.{1} == '{3}' do find;", BaseTestDir, Hash.Algorithm, TrailPartStringHash, HashString);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void ValidateFileSuccess()
        {
            IList<string> results = RunQuery(ValidationQueryTemplate, NotEmptyFile, Hash.Algorithm, HashString);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0],
                        Is.EqualTo(string.Format(FileResultTpl, NotEmptyFile, "File is valid", InitialString.Length)));
        }

        [Test]
        public void ValidateFileFailure()
        {
            IList<string> results = RunQuery(ValidationQueryTemplate, NotEmptyFile, Hash.Algorithm, TrailPartStringHash);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0],
                        Is.EqualTo(string.Format(FileResultTpl, NotEmptyFile, "File is invalid", InitialString.Length)));
        }
    }
}