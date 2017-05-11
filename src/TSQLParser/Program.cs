using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSQLParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = new Parser();
            p.RunTest();
        }

    }

    class Parser
    {
        public void RunTest()
        {
            string script = "select * from dbo.Mytable where columnName = 0; delete from dbo.Mytable where columnName = 0";
            var sqlScript = ParseScript(script);

            PrintStatements(sqlScript);
        }

        public TSqlScript ParseScript(string script)
        {
            IList<ParseError> parseErrors;
            TSql100Parser tsqlParser = new TSql100Parser(true);
            TSqlFragment fragment;
            using (StringReader stringReader = new StringReader(script))
            {
                fragment = (TSqlFragment)tsqlParser.Parse(stringReader, out parseErrors);
            }
            if (parseErrors.Count > 0)
            {
                var retMessage = string.Empty;
                foreach (var error in parseErrors)
                {
                    retMessage += error.Number + " - " + error.Message + " - position: " + error.Offset + ";\r\n";
                }
                Console.WriteLine(retMessage); 
            }
            return (TSqlScript)fragment;

        }

        public void PrintStatements(TSqlScript tsqlScript)
        {

            if (tsqlScript != null)
            {
                foreach (TSqlBatch batch in tsqlScript.Batches)
                {
                    if (batch.Statements.Count == 0) continue;

                    foreach (TSqlStatement statement in batch.Statements)
                    {
                        var peter = new
                        {
                            StatementType = statement.GetType().ToString(),
                            Tokens = statement.ScriptTokenStream.Select(s => new { TokenType = s.TokenType, Text = s.Text })
                        };
                        Console.WriteLine(peter);
                        
                        //Console.WriteLine(string.Format("{0}\r\n", statement.GetType().ToString()));
                        
                        //Console.WriteLine(string.Join("\n", statement.ScriptTokenStream.Select(s => new { TokenType = s.TokenType, Text=s.Text })));
                    }
                }

            }
        }
    }

    class OwnVisitor : TSqlFragmentVisitor
    {
        public override void ExplicitVisit(SelectStatement node)
        {
            QuerySpecification querySpecification = node.QueryExpression as QuerySpecification;

            FromClause fromClause = querySpecification.FromClause;
            // There could be more than one TableReference!
            // TableReference is not sure to be a NamedTableReference, could be as example a QueryDerivedTable
            NamedTableReference namedTableReference = fromClause.TableReferences[0] as NamedTableReference;
            TableReferenceWithAlias tableReferenceWithAlias = fromClause.TableReferences[0] as TableReferenceWithAlias;
            string baseIdentifier = namedTableReference?.SchemaObject.BaseIdentifier?.Value;
            string schemaIdentifier = namedTableReference?.SchemaObject.SchemaIdentifier?.Value;
            string databaseIdentifier = namedTableReference?.SchemaObject.DatabaseIdentifier?.Value;
            string serverIdentifier = namedTableReference?.SchemaObject.ServerIdentifier?.Value;
            string alias = tableReferenceWithAlias.Alias?.Value;
            Console.WriteLine("From:");
            Console.WriteLine($"  {"Server:",-10} {serverIdentifier}");
            Console.WriteLine($"  {"Database:",-10} {databaseIdentifier}");
            Console.WriteLine($"  {"Schema:",-10} {schemaIdentifier}");
            Console.WriteLine($"  {"Table:",-10} {baseIdentifier}");
            Console.WriteLine($"  {"Alias:",-10} {alias}");



            // Example of changing the alias:
            //(fromClause.TableReferences[0] as NamedTableReference).Alias = new Identifier() { Value = baseIdentifier[0].ToString() };

            Console.WriteLine("Statement:");
            Console.WriteLine(node.ToSqlString().Indent(2));

            Console.WriteLine("¯".Multiply(40));

            base.ExplicitVisit(node);
        }
    }

    public static class TSqlDomHelpers
    {
        public static string ToSourceSqlString(this TSqlFragment fragment)
        {
            StringBuilder sqlText = new StringBuilder();
            for (int i = fragment.FirstTokenIndex; i <= fragment.LastTokenIndex; i++)
            {
                sqlText.Append(fragment.ScriptTokenStream[i].Text);
            }
            return sqlText.ToString();
        }

        public static string ToSqlString(this TSqlFragment fragment)
        {
            SqlScriptGenerator generator = new Sql120ScriptGenerator();
            string sql;
            generator.GenerateScript(fragment, out sql);
            return sql;
        }
    }
    public static class StringHelpers
    {
        public static string Indent(this string Source, int NumberOfSpaces)
        {
            string indent = new string (' ', NumberOfSpaces);
            return indent + Source.Replace("\n", "\n" + indent);
        }
        public static string Multiply(this string Source, int Multiplier)
        {
            StringBuilder stringBuilder = new StringBuilder(Multiplier * Source.Length);
            for (int i = 0; i < Multiplier; i++)
            {
                stringBuilder.Append(Source);
            }
            return stringBuilder.ToString();
        }
    }
}
