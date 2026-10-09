using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ORD.Core.Database;
using ORD.Core.Engine;
using ORD.Core.Models;

var builder = WebApplication.CreateBuilder(args);

// 싱글톤 서비스 등록
builder.Services.AddSingleton<UnitRepository>();
builder.Services.AddSingleton<NavigationRepository>();
builder.Services.AddSingleton<SynergyEvaluator>();
builder.Services.AddSingleton<BuildRecommender>();
builder.Services.AddSingleton<NavigationRecommender>();

// CORS 허용 (로컬 브라우저 및 향후 외부 배포 연동)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();

// ==========================================
// [REST API 엔드포인트]
// ==========================================

// 1. 전체 유닛 목록 반환
app.MapGet("/api/units", (UnitRepository repo) =>
{
    return Results.Ok(repo.GetAll());
});

// 2. 전체 항법 목록 반환
app.MapGet("/api/navigations", (NavigationRepository repo) =>
{
    return Results.Ok(repo.GetAll());
});

// 3. 현재 덱 분석, 시너지 추천 및 21라운드 항법 추천 API
app.MapPost("/api/evaluate", (DeckRequest request, UnitRepository repo, SynergyEvaluator evaluator, BuildRecommender recommender, NavigationRecommender navRecommender) =>
{
    var currentUnits = new List<OrdUnit>();
    if (request.UnitIds != null)
    {
        foreach (var id in request.UnitIds)
        {
            var unit = repo.GetById(id);
            if (unit != null)
            {
                currentUnits.Add(unit);
            }
        }
    }

    int targetRound = request.TargetRound > 0 ? request.TargetRound : NightmareCriteria.V2323_EndRound;
    var analysis = evaluator.Evaluate(currentUnits, targetRound);
    var recommendations = recommender.RecommendNextBuilds(analysis, currentUnits, 8);
    var navigationRecommendations = navRecommender.RecommendForRound21(analysis, currentUnits);

    return Results.Ok(new
    {
        Analysis = analysis,
        Recommendations = recommendations,
        NavigationRecommendations = navigationRecommendations,
        CurrentUnits = currentUnits,
        Criteria = new
        {
            EndRound = targetRound,
            NightmareArmor = NightmareCriteria.V2323_NightmareArmor,
            ShinArmor = NightmareCriteria.V2323_ShinArmor,
            FullSlowCap = NightmareCriteria.V2323_FullSlowCap
        }
    });
});

Console.WriteLine("===================================================================");
Console.WriteLine("  ORD Synergy & Build Helper (원랜디 v2.323 스마트 도우미 서버)  ");
Console.WriteLine("  대시보드 주소: http://localhost:5000                            ");
Console.WriteLine("===================================================================");

app.Run("http://127.0.0.1:5000");

public record DeckRequest(
    [property: JsonPropertyName("unitIds")] List<string>? UnitIds,
    [property: JsonPropertyName("targetRound")] int TargetRound = 65
);
