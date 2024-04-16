using MathNet.Numerics.LinearAlgebra;
using Azure.AI.OpenAI;
using Qdrant.Client;
using Qdrant.Client.Grpc;

//open ai key
var apiKey = "👉openai_api_key";
var openAIClient = new OpenAIClient(apiKey);

//連線到 Qdrant DB
var Qdrant_collection_name = "test_collection";
var qdrantClient = new QdrantClient("localhost", 6334);
var isExist = await qdrantClient.CollectionExistsAsync(Qdrant_collection_name);

//如果不存在，則建立一個新的集合
if (!isExist)
{
    // 建立一個新的集合
    await qdrantClient.CreateCollectionAsync(
        collectionName: Qdrant_collection_name,
        vectorsConfig: new VectorParams { Size = 1536, Distance = Distance.Dot }
    );
    // 取得問題列表
    var Questions = GetQuestions();

    var points = new List<PointStruct>();
    int i = 0;

    Console.WriteLine($"\n載入問題清單...");
    foreach (var item in Questions)
    {
        // 將問題insert到集合
        points.Add(
             new()
             {
                 Id = (ulong)i++, // 選擇一個唯一ID
                 Payload = { ["type"] = "CDC", ["utterance"] = item }, // 其他metadata
                 // 取得問題的嵌入向量
                 Vectors = GetEmbeddings(openAIClient, item).ToArray()  // 確保向量是以 List<float> 的形式
             }
            );

        Console.WriteLine($"processing question {i}:{item}");
    }
    // 將問題insert到 Qdrant DB
    await qdrantClient.UpsertAsync(collectionName: Qdrant_collection_name, points);
}

// 問題搜索
string? question = "";
do
{
    //輸入問題   
    Console.Write("\n\n請輸入與施打疫苗有關的問題('q' 離開)：");
    question = Console.ReadLine();
    if (string.IsNullOrEmpty(question) || question == "q" || question == "") return;
    // 搜索最相關的問題
    var searchResult = await qdrantClient.SearchAsync(
       collectionName: Qdrant_collection_name,
       vector: GetEmbeddings(openAIClient, question).ToArray(),
       limit: 3,
       payloadSelector: true
    );

    Console.WriteLine("\n\n 列出最相關的問題：");
    foreach (var item in searchResult)
    {
        Console.WriteLine($"Score:{item.Score} utterance:{item.Payload["utterance"].StringValue}");
    }

} while (true);


// 取得嵌入向量
static List<float> GetEmbeddings(OpenAIClient client, string utterance)
{
    List<float> embeddingVector = new List<float>();

    // 調用API並獲取嵌入向量
    EmbeddingsOptions embeddingOptions = new EmbeddingsOptions()
    {
        DeploymentName = "text-embedding-3-small",
        Input = { utterance },
    };

    // 調用API並獲取嵌入向量
    var returnValue = client.GetEmbeddingsAsync(embeddingOptions);

    // 創建一個新的列表來存儲嵌入向量
    foreach (var item in returnValue.Result.Value.Data[0].Embedding.ToArray())
    {
        embeddingVector.Add(item);
    }

    return embeddingVector;
}

// 取得問題列表
string[] GetQuestions()
{
    string[] questions = new string[]
        {
            "COVID-19疫苗該接種幾劑?",
            "可以選擇疫苗的廠牌嗎?",
            "COVID-19疫苗需要以同廠牌完成接種嗎?是否可交替廠牌?",
            "接種COVID-19疫苗與其他非COVID-19疫苗要間隔多久?",
            "COVID-19疫苗適合接種在哪一個年齡層?",
            "什麼時候輪到我接種？",
            "如果我不是COVID-19疫苗計畫中的接種對象、或尚未安排到接種COVID-19疫苗，我要怎麼保護自己？",
            "若我曾感染過COVID-19，我仍要接種COVID-19疫苗嗎？",
            "我對雞蛋或牛奶過敏，可以接種COVID-19疫苗嗎？",
            "我有過敏體質，可以接種COVID-19疫苗嗎?",
            "如果我在建議接種第二劑疫苗的時間未接種到疫苗怎麼辦？",
            "基礎加強劑是什麼? 什麼對象可以打基礎加強劑？",
            "追加劑是什麼? 什麼對象可以打追加劑？",
            "孕婦可否接種COVID-19疫苗?",
            "哺乳中的婦女可否接種COVID-19疫苗?",
            "兒童可否接種COVID-19疫苗?",
            "接種COVID-19疫苗有什麼注意事項？",
            "我曾經(或正在)參加COVID-19疫苗的臨床試驗, 還可以再接種COVID-19疫苗嗎?",
            "哪些人不適合接種COVID-19疫苗 (接種禁忌症)?",
            "接種COVID-19疫苗後要注意什麼？",
            "當COVID-19疫苗須接種兩劑為完整接種時，若發生第一劑接種後，但尚未到第二劑接種時，確診為SARS-CoV-2感染，康復後，應何時接種同廠牌之第二劑疫苗？",
            "需完成兩劑接種之COVID-19疫苗，若接種第一劑COVID-19疫苗後，如出現症狀經醫師評估懷疑與疫苗施打有關並通報至疾病管制署疫苗不良事件通報系統(VAERS)者，經醫師評估不適合再接種同樣疫苗時，第二劑不同廠牌之COVID-19疫苗，應間隔多久再接種？",
            "曾有血栓病史或罹患易有血栓風險的慢性疾病者，能否接種AstraZeneca (AZ) COVID-19疫苗?",
            "有心臟病史，接種mRNA COVID-19疫苗會增加心肌炎/心包膜炎的風險嗎?",
            "哪些人接種mRNA COVID-19疫苗前應先諮詢心臟科醫師?",
            "接種第㇐劑 mRNA COVID-19 疫苗後發生心肌炎/心包膜炎的人，可以接種第二劑嗎?",
            "接種mRNA COVID-19疫苗前後要注意哪些事?",
            "如果COVID-19疫苗施打的間隔不足，該怎麼辦？",
            "如果COVID-19疫苗施打的劑量不足，該怎麼辦？",
            "如果COVID-19疫苗施打的劑量過多，該怎麼辦？",
            "什麼對象可以打第二次追加劑？可以接種什麼廠牌？",
            "接種單位如遇個案體重低於同年齡平均者，是否以兒童劑型或減少劑量之COVID-19疫苗接種?"
        };
    return questions;
}

