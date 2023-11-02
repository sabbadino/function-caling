using FunctionCalling.Controllers.Dtos;
using FunctionCalling.ExternalServices.Mdm;
using FunctionCalling.Ioc;

namespace FunctionCalling.Repository.Quotes
{
    public interface IQuoteRepository
    {
        List<SubmittedQuote> GetUserQuotes(string username, QuoteStatus? status);
        void Add(SubmittedQuote quote);
    }
    public class QuoteRepository : IQuoteRepository,ISingletonScope
    {
        private readonly IMdm _mdm;
        private readonly List<string> _createdBy;
        private readonly List<SubmittedQuote> _preLoadedQuotes= new List<SubmittedQuote>();
        private readonly Random _random = new Random();

        private readonly Dictionary<string, List<SubmittedQuote>> _quotes = new Dictionary<string, List<SubmittedQuote>>();

        public QuoteRepository(IMdm mdm)
        {
            _mdm = mdm;
            _createdBy = new List<string> { "enrico.sabbadin@msc.com", "marco@msc.com", "giorgio@msc.com", "franco@msc.com" };
            var ports = _mdm.GetAllPorts().ToList();
            Enumerable.Range(0, _random.Next(2, 5)).ToList().ForEach(i =>
            {
                _preLoadedQuotes.Add(new SubmittedQuote
                {
                    Amount = RandomNumber(500, 1456),
                    Currency = "USD",
                    ShippingWindowsFrom = DateTime.Now.AddDays(5),
                    ShippingWindowsTo = DateTime.Now.AddDays(35),
                    TransitDays = RandomNumber(13, 21),
                    ContainerType = RandomContainerType(),
                    Destination = ports[_random.Next(0,ports.Count)].PortVersion.Name,
                    Origin = ports[_random.Next(0, ports.Count)].PortVersion.Name
                });
            });
        }
        private QuoteStatus RandomQuoteStatus()
        {
            var values = Enum.GetValues<QuoteStatus>();
            return values[_random.Next(0, values.Length)];
        }

        private ContainerType RandomContainerType()
        {
            var values = Enum.GetValues<ContainerType>();
            return values[_random.Next(0, values.Length)];
        }
        private int RandomNumber(int from, int to)
        {
            return _random.Next(from, to);

        }
        private string RandomEmail()
        {
            return _createdBy[_random.Next(0, _createdBy.Count)];
        }
        public List<SubmittedQuote> GetUserQuotes(string username, QuoteStatus? status)
        {
            var quotes = _preLoadedQuotes.Where(b=> status==null ||  b.Status == status).ToList();   
            if (_quotes.TryGetValue(username, out var newQuotes))
            {
                var ret = newQuotes.Where(b=> status ==null || b.Status == status.Value).ToList();
                quotes.AddRange(ret);
            }

            return quotes;
        }

        public void Add(SubmittedQuote quote)
        {
            if (!_quotes.ContainsKey(quote.Email))
            {
                _quotes.Add(quote.Email, new List<SubmittedQuote>());
            }
            _quotes[quote.Email].Add(quote);
        }
    }
}
