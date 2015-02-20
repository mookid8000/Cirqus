namespace EnergyProjects.Domain.Utilities
{
    public interface IWriter
    {
        IWriter Indent();
        IWriter Unindent();
        IWriter NewLine();
        IWriter Block(string header);
        IWriter EndBlock();
        IWriter Write(object obj);
        IWriter Write(string str);
    }
}