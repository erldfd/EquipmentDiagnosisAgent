using System;
using System.Net.Http.Json;
using System.Text.Json;
using EquipmentDiagnosisAgent;

EquipmentManager equipmentManager = new EquipmentManager();

// foreach (Equipment equipment in equipmentManager.GetAllEquipments())
// {
//     Console.WriteLine($"ID: {equipment.Id}, Name: {equipment.Name}, Status: {equipment.Status}, Temperature: {equipment.Temperature} Celsius");
// }

// Equipment? target = equipmentManager.GetEquipmentById(2);

// if (target != null)
// {
//     Console.WriteLine($"찾은 장비 - ID: {target.Id}, Name: {target.Name}, Status: {target.Status}, Temperature: {target.Temperature} Celsius");
// }
// else
// {
//     Console.WriteLine("장비를 찾을 수 없습니다.");
// }


List<object> newMessages = new List<object>
{
    new
    {
        role = "system",
        content = "장비 정보를 묻는 질문에는 제공된 도구를 사용하세요. 정보를 추측하지 마세요."
    }
};

object[] newTools = new[]
{
    new
    {
        type = "function",
        function = new
        {
            name = "GetEquipmentById",
            description = "장비 ID를 기반으로 장비 정보를 검색합니다.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    id = new
                    {
                        type = "integer",
                        description = "검색할 장비의 ID"
                    }
                },

                required = new[] { "id" }
            }
        }
    }
};

using HttpClient client = new HttpClient();
client.Timeout = TimeSpan.FromSeconds(30);

const int MAX_ITERATIONS = 5;
for (int i = 0; i < MAX_ITERATIONS; i++)
{
    Console.WriteLine($"{i + 1}번째 질문을 입력하세요.");
    Console.Write("질문 : ");
    string? input = Console.ReadLine();

    if (string.IsNullOrEmpty(input))
    {
        Console.WriteLine("입력값이 없습니다. 입력해주세요.");
        continue;
    }

    newMessages.Add(new
    {
        role = "user",
        content = input
    });


    Console.WriteLine("질문을 처리중입니다...");
    Console.WriteLine();

    using HttpResponseMessage response = await client.PostAsJsonAsync(

        "http://localhost:11434/api/chat",
        new
        {
            model = "qwen3.5:4b",
            messages = newMessages,
            tools = newTools,

            stream = false,
            think = false
        }
    );

    // 응답이 실패하면 예외를 일으키는 함수
    response.EnsureSuccessStatusCode();
    // Console.WriteLine($"{response.ToString()}");

    string json = await response.Content.ReadAsStringAsync();
    // Console.WriteLine($"응답 JSON : {json}");

    using JsonDocument document = JsonDocument.Parse(json);

    JsonElement root = document.RootElement;

    string? answer = null;

    JsonElement assistantMessage = root.GetProperty("message");

    newMessages.Add(assistantMessage.Clone());

    if (assistantMessage.TryGetProperty("tool_calls",out JsonElement toolCalls) && toolCalls.GetArrayLength() > 0)
    {
        foreach (JsonElement toolCall in toolCalls.EnumerateArray())
        {
            JsonElement function = toolCall.GetProperty("function");

            string? name = function.GetProperty("name").GetString();
            string argument = function.GetProperty("arguments").GetRawText();

            string result;

            if(name == "GetEquipmentById")
            {
                JsonDocument argumentDoc = JsonDocument.Parse(argument);
                int id = argumentDoc.RootElement.GetProperty("id").GetInt32();

                Equipment? equipment = equipmentManager.GetEquipmentById(id);

                if (equipment != null)
                {
                    result = JsonSerializer.Serialize(new
                    {
                        equipment.Id,
                        equipment.Name,
                        Status = equipment.Status.ToString(),
                        equipment.Temperature,
                        equipment.AlarmCode
                        
                    });
                }
                else
                {
                    result = JsonSerializer.Serialize(new
                    {
                        Error = "장비를 찾을 수 없습니다."
                    });
                }
            }
            else
            {
                result = JsonSerializer.Serialize(new
                {
                    Error = "알 수 없는 함수 호출입니다."
                });
            }

            Console.WriteLine($"함수 이름 : {name}");
            Console.WriteLine($"함수 인자 : {argument}");

            newMessages.Add(new
            {
                role = "tool",
                tool_name = name,
                content = result
            });
        }

        using HttpResponseMessage finalResponse = await client.PostAsJsonAsync(
            "http://localhost:11434/api/chat",
            new
            {
                model = "qwen3.5:4b",
                messages = newMessages,
                stream = false,
                think = false
            }
        );

        finalResponse.EnsureSuccessStatusCode();

        string finalJson = await finalResponse.Content.ReadAsStringAsync();

        using JsonDocument finalDocument = JsonDocument.Parse(finalJson);

        answer = finalDocument.RootElement.GetProperty("message").GetProperty("content").GetString();

    }
    else
    {
        answer = assistantMessage.GetProperty("content").GetString();
    }

    Console.WriteLine();
    Console.WriteLine($"AI 답변 : {answer}");

    Console.WriteLine();
    // string total = root.GetRawText();
    // Console.WriteLine($"전체 : {total}");
    // Console.WriteLine();

    double totalSeconds = root.GetProperty("total_duration").GetDouble() / 1_000_000_000.0;
    double loadSeconds = root.GetProperty("load_duration").GetDouble() / 1_000_000_000.0;

    Console.WriteLine($"전체 소요 시간 : {totalSeconds:F2}초");
    Console.WriteLine($"모델 로딩 시간 : {loadSeconds:F2}초");
    Console.WriteLine();

}

Console.WriteLine("대화 내역");

foreach (object message in newMessages)
{
    Console.WriteLine($"{((dynamic)message).role}: {((dynamic)message).content}");
}