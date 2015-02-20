using System;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using EnergyProjects.Domain.Utilities;

namespace EnergyProjects.Tests
{
    public class ConsoleWriter : IWriter
    {
        const string indent = "  ";

        int indentation;
        int cursor;
        int margin;
        string current;

        public IWriter Indent()
        {
            indentation++;
            return this;
        }

        public IWriter Unindent()
        {
            if (indentation > 0)
            {
                indentation--;
            }
            return this;
        }

        public IWriter NewLine()
        {
            cursor = 0;
            margin++;
            Console.WriteLine();
            return this;
        }

        public IWriter Write(object obj)
        {
            //Write(new EventFormatter().Render(obj));
            return this;
        }

        public IWriter Write(string str)
        {
            if (cursor == 0)
            {
                Console.Write(string.Join("", Enumerable.Repeat(indent, indentation)));
            }

            margin = 0;
            cursor += str.Length;
            Console.Write(str);
            return this;
        }

        public IWriter Block(string header)
        {
            if (current == header)
                return this;

            if (current != null)
            {
                EndBlock();
            }

            current = header;

            if (cursor > 0 || margin > 0)
            {
                if (cursor > 0)
                {
                    NewLine();
                }

                while (margin < 2)
                {
                    NewLine();
                }
            }

            Write(header);
            NewLine();
            Indent();
            return this;
        }

        public IWriter EndBlock()
        {
            current = null;
            Unindent();
            return this;
        }
    }
}