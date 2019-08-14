using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CSVWebForms
{
    public class CSVHelperClass : IDisposable
    {
        #region Static Members
        public static CsvDefinition DefaultCsvDefinition { get; set; }
        public static bool UseLambdas { get; set; }
        public static bool UseTasks { get; set; }
        public static bool FastIndexOfAny { get; set; }

        static CSVHelperClass()
        {
            DefaultCsvDefinition = new CsvDefinition
            {
                EndOfLine = "\r\n",
                FieldSeparator = ',',
                TextQualifier = '"'
            };
            UseLambdas = true;
            UseTasks = true;
            FastIndexOfAny = true;

        }

        #endregion

        internal protected Stream BaseStream;
        protected static DateTime DateTimeZero = new DateTime();

        public static IEnumerable<T> Read<T>(CsvSource csvSource) where T : new()
        {
            var csvFileReader = new CsvFileReader<T>(csvSource);
            return (IEnumerable<T>)csvFileReader;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            
        }
    }


    internal class CsvFileReader<T> : CSVHelperClass, IEnumerable<T>, IEnumerator<T>
    where T : new()
    {
        private readonly Dictionary<Type, List<Action<T, String>>> allSetters = new Dictionary<Type, List<Action<T, String>>>();
        private string[] columns;
        private char curChar;
        private int len;
        private string line;
        private int pos;
        private T record;
        private readonly char fieldSeparator;
        private readonly TextReader textReader;
        private readonly char textQualifier;
        private readonly StringBuilder parseFieldResult = new StringBuilder();

        public CsvFileReader(CsvSource csvSource) : this(csvSource, null)
        {
        }

        public CsvFileReader(CsvSource csvSource, CsvDefinition csvDefinition)
        {
            var streamReader = csvSource.TextReader as StreamReader;
            if (streamReader != null)
                this.BaseStream = streamReader.BaseStream;
            if (csvDefinition == null)
                csvDefinition = DefaultCsvDefinition;
            this.fieldSeparator = csvDefinition.FieldSeparator;
            this.textQualifier = csvDefinition.TextQualifier;

            this.textReader = csvSource.TextReader;

            this.ReadHeader(csvDefinition.Header);

        }

        public T Current
        {
            get { return this.record; }
        }

        object IEnumerator.Current
        {
            get { return this.Current; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            this.ReadNextLine();
            if (this.line == null && (this.line = this.textReader.ReadLine()) == null)
            {
                this.record = default(T);
            }
            else
            {
                this.record = new T();
                Type recordType = typeof(T);
                List<Action<T, String>> setters;
                if (!this.allSetters.TryGetValue(recordType, out setters))
                {
                    setters = this.CreateSetters();
                    this.allSetters[recordType] = setters;
                }

                var fieldValues = new string[setters.Count];
                for (int i = 0; i < setters.Count; i++)
                {
                    fieldValues[i] = this.ParseField();
                    if (this.curChar == this.fieldSeparator)
                        this.NextChar();
                    else
                        break;
                }
                for (int i = 0; i < setters.Count; i++)
                {
                    var setter = setters[i];
                    if (setter != null)
                    {
                        setter(this.record, fieldValues[i]);
                    }
                }
            }
            return (this.record != null);
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void ReadHeader(string header)
        {
            if (header == null)
            {
                this.ReadNextLine();
            }
            else
            {                
                this.line = header;
                this.pos = -1;
                this.len = this.line.Length;
                this.NextChar();
            }

            var readColumns = new List<string>();
            string columnName;
            while ((columnName = this.ParseField()) != null)
            {
                readColumns.Add(columnName);
                if (this.curChar == this.fieldSeparator)
                    this.NextChar();
                else
                    break;
            }
            this.columns = readColumns.ToArray();
        }

        private void ReadNextLine()
        {
            this.line = this.textReader.ReadLine();
            this.pos = -1;
            if (this.line == null)
            {
                this.len = 0;
                this.curChar = '\0';
            }
            else
            {
                this.len = this.line.Length;
                this.NextChar();
            }
        }

        private void NextChar()
        {
            if (this.pos < this.len)
            {
                this.pos++;
                this.curChar = this.pos < this.len ? this.line[this.pos] : '\0';
            }
        }

        private string ParseField()
        {
            parseFieldResult.Length = 0;
            if (this.line == null || this.pos >= this.len)
                return null;
            while (this.curChar == ' ' || this.curChar == '\t')
            {
                this.NextChar();
            }
            if (this.curChar == this.textQualifier)
            {
                this.NextChar();
                while (this.curChar != 0)
                {
                    if (this.curChar == this.textQualifier)
                    {
                        this.NextChar();
                        if (this.curChar == this.textQualifier)
                        {
                            this.NextChar();
                            parseFieldResult.Append(this.textQualifier);
                        }
                        else
                            return parseFieldResult.ToString();
                    }
                    else if (this.curChar == '\0')
                    {
                        if (this.line == null)
                            return parseFieldResult.ToString();
                        this.ReadNextLine();
                    }
                    else
                    {
                        parseFieldResult.Append(this.curChar);
                        this.NextChar();
                    }
                }
            }
            else
            {
                while (this.curChar != 0 && this.curChar != this.fieldSeparator && this.curChar != '\r' && this.curChar != '\n')
                {
                    parseFieldResult.Append(this.curChar);
                    this.NextChar();
                }
            }
            return parseFieldResult.ToString();
        }

        private List<Action<T, string>> CreateSetters()
        {
            var list = new List<Action<T, string>>();
            for (int i = 0; i < this.columns.Length; i++)
            {
                string columnName = this.columns[i];
                Action<T, string> action = null;
                if (columnName.IndexOf(' ') >= 0)
                    columnName = columnName.Replace(" ", "");
                action = FindSetter(columnName, false) ?? FindSetter(columnName, true);

                list.Add(action);
            }
            return list;
        }

        private static Action<T, string> FindSetter(string c, bool staticMember)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase | (staticMember ? BindingFlags.Static : BindingFlags.Instance);
            Action<T, string> action = null;
            PropertyInfo pi = typeof(T).GetProperty(c, flags);
            if (pi != null)
            {
                var pFunc = DataTypesToObject(pi.PropertyType);
                action = EmitSetValueAction(pi, pFunc);
            }
            FieldInfo fi = typeof(T).GetField(c, flags);
            if (fi != null)
            {
                var fFunc = DataTypesToObject(fi.FieldType);
                action = EmitSetValueAction(fi, fFunc);
            }
            return action;
        }

        private static Func<string, object> DataTypesToObject(Type propertyType)
        {
            if (propertyType == typeof(string))
                return (s) => s ?? String.Empty;
            else if (propertyType == typeof(int))
                return (s) => String.IsNullOrEmpty(s) ? 0 : int.Parse(s);            
            if (propertyType == typeof(DateTime))
                return (s) => String.IsNullOrEmpty(s) ? DateTimeZero : DateTime.Parse(s);
            else
                throw new NotImplementedException();
        }

        private static Action<T, string> EmitSetValueAction(MemberInfo mi, Func<string, object> func)
        {
            ParameterExpression paramExpObj = Expression.Parameter(typeof(object), "obj");
            ParameterExpression paramExpT = Expression.Parameter(typeof(T), "instance");

            {
                var pi = mi as PropertyInfo;
                if (pi != null)
                {
                    if (CSVHelperClass.UseLambdas)
                    {
                        var callExpr = Expression.Call(
                            paramExpT,
                            pi.GetSetMethod(),
                            Expression.ConvertChecked(paramExpObj, pi.PropertyType));
                        var setter = Expression.Lambda<Action<T, object>>(
                            callExpr,
                            paramExpT,
                            paramExpObj).Compile();
                        return (o, s) => setter(o, func(s));
                    }

                    return (o, v) => pi.SetValue(o, (object)func(v), null);

                }
            }
            {
                var fi = mi as FieldInfo;
                if (fi != null)
                {
                    if (CSVHelperClass.UseLambdas)
                    {
                        var valueExp = Expression.ConvertChecked(paramExpObj, fi.FieldType);

                        MemberExpression fieldExp = Expression.Field(paramExpT, fi);
                        BinaryExpression assignExp = Expression.Assign(fieldExp, valueExp);

                        var setter = Expression.Lambda<Action<T, object>>
                            (assignExp, paramExpT, paramExpObj).Compile();

                        return (o, s) => setter(o, func(s));
                    }

                    return ((o, v) => fi.SetValue(o, func(v)));
                }
            }
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.textReader.Dispose();
            }
        }
    }

    public class CsvDefinition
    {
        public string Header { get; set; }
        public char FieldSeparator { get; set; }
        public char TextQualifier { get; set; }
        public IEnumerable<String> Columns { get; set; }
        public string EndOfLine { get; set; }

        public CsvDefinition()
        {
            if (CSVHelperClass.DefaultCsvDefinition != null)
            {
                FieldSeparator = CSVHelperClass.DefaultCsvDefinition.FieldSeparator;
                TextQualifier = CSVHelperClass.DefaultCsvDefinition.TextQualifier;
                EndOfLine = CSVHelperClass.DefaultCsvDefinition.EndOfLine;
            }
        }
    }

    public class CsvSource
    {
        public readonly TextReader TextReader;

        public static implicit operator CsvSource(CSVHelperClass csvFile)
        {
            return new CsvSource(csvFile);
        }

        public static implicit operator CsvSource(string path)
        {
            return new CsvSource(path);
        }

        public static implicit operator CsvSource(TextReader textReader)
        {
            return new CsvSource(textReader);
        }

        public CsvSource(TextReader textReader)
        {
            this.TextReader = textReader;
        }

        public CsvSource(Stream stream)
        {
            this.TextReader = new StreamReader(stream);
        }

        public CsvSource(string path)
        {
            this.TextReader = new StreamReader(path);
        }

        public CsvSource(CSVHelperClass csvFile)
        {
            this.TextReader = new StreamReader(csvFile.BaseStream);
        }
    }

    public class CsvDestination
    {
        public StreamWriter StreamWriter;

        public static implicit operator CsvDestination(string path)
        {
            return new CsvDestination(path);
        }
        private CsvDestination(StreamWriter streamWriter)
        {
            this.StreamWriter = streamWriter;
        }

        private CsvDestination(Stream stream)
        {
            this.StreamWriter = new StreamWriter(stream);
        }

        public CsvDestination(string fullName)
        {
            FixCsvFileName(ref fullName);
            this.StreamWriter = new StreamWriter(fullName);
        }

        private static void FixCsvFileName(ref string fullName)
        {
            fullName = Path.GetFullPath(fullName);
            var path = Path.GetDirectoryName(fullName);
            if (path != null && !Directory.Exists(path))
                Directory.CreateDirectory(path);
            if (!String.Equals(Path.GetExtension(fullName), ".csv"))
                fullName += ".csv";
        }
    }

    public class CsvIgnorePropertyAttribute : Attribute
    {
        public override string ToString()
        {
            return "Ignore Property";
        }
    }

}