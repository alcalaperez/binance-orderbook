namespace BinanceOrderbooks.Model
{
    public class CommandLineArgs
    {
        public CommandLineArgs(string instrument, int size)
        {
            Instrument = instrument;
            Size = size;
        }

        public string Instrument { get; set; }
        public int Size { get; set; }

    }
}