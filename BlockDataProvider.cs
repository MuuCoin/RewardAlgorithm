namespace MuuCoinRewards
{
    internal class BlockDataProvider
    {
        public static async Task<List<ConnectedUsers>> GetCurrencyHolders(string currency)
        {
            var graphQLClient = new GraphQLHttpClient("https://graphql.bitquery.io/", new GraphQL.Client.Serializer.Newtonsoft.NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Add("X-API-KEY", "APIKEY");
            var transactionQuery = new GraphQLRequest
            {
                Query = "{ ethereum(network: bsc) { transfers( currency: {is: \"" + currency + "\"} ) { receiver { address } amount count } } }"
            };
            var users = new List<ConnectedUsers>();
            var graphQLResponse = await graphQLClient.SendQueryAsync<MuuHolders>(transactionQuery);
            var holders = graphQLResponse.Data.ethereum.transfers.Select(x => new ConnectedUsers { Account = x.receiver.address, IsWinner = false }).ToList();


            return holders;
        }

        public static async Task<(bool, string, BigInteger)> CheckValidWinner(List<ConnectedUsers> getHodlers, string currency)
        {
            var result = false;
            var random = new Random();
            var index = random.Next(0, getHodlers.Count);
            var element = getHodlers.ElementAt(index);
            var web3 = new Nethereum.Web3.Web3("https://bsc-dataseed.binance.org"); //testnet
 
            var balanceOfFunctionMessage = new BalanceOfFunction()
            {
                Owner = element.Account,
            };

            var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
            var balance = await balanceHandler.QueryAsync<BigInteger>(currency, balanceOfFunctionMessage);
            if (balance > 0)
                result = true;
            else
                result = false;

            Console.WriteLine(balance.ToString());

            Console.WriteLine(web3);
            RewardSplitter.ConnectedUsers.ElementAt(index).IsWinner = true;
            var totalReward = await BlockDataProvider.GetPoolReward();

            return new(result, balanceOfFunctionMessage.Owner, totalReward);
        }

        public static async Task<BigInteger> GetPoolReward()
        {
            var context = new muucoinContext();
            var publicKey = "0x09799b077BdDd3AA6690d03F5DC9458Fdea6BD69";
            
            var web3 = new Nethereum.Web3.Web3("https://bsc-dataseed.binance.org/"); //testnet
 
            var balanceOfFunctionMessage = new BalanceOfFunction()
            {
                Owner = publicKey,
            };

            var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
            var balance = await balanceHandler.QueryAsync<BigInteger>(context.SystemDefaults.FirstOrDefault().ContractAddress, balanceOfFunctionMessage);


            return balance;
        }


    }
}
