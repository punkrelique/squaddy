open System
open System.Net.Http
open System.Net.Http.Json

let args = fsi.CommandLineArgs[1..]


if args.Length = 0 then
    printf "Username was not provided"
    Environment.Exit(-1)
    
let username = args[0]

let caseComparison =
    match Array.tryItem 1 args with
    | Some value when value = "ignorecase" -> StringComparison.OrdinalIgnoreCase
    | Some value when value = "followcase" -> StringComparison.Ordinal
    | _                                    -> StringComparison.OrdinalIgnoreCase
    
let findString =
    match Array.tryItem 2 args with
    | Some value when value = "contains" -> fun (str1: string) (str2: string) -> str1.Contains(str2, caseComparison)
    | Some value when value = "exactly"  -> fun (str1: string) (str2: string) -> str1.Equals(str2, caseComparison)
    | _                                  -> fun (str1: string) (str2: string) -> str1.Contains(str2, caseComparison)

let steamApiKey = Environment.GetEnvironmentVariable("SQUADDY_STEAM_API_KEY")
if String.IsNullOrEmpty(steamApiKey) then
    printfn "SQUADDY_STEAM_API_KEY environment variable is not set"
    Environment.Exit(-1)

type MySquadStats = { data: seq<{| lastName: string; steamID: string |}>; status: string; message: string }
let mySquadStatsUrlTemplate username = $"https://api.mysquadstats.com/players?search={username}"

let getPlayerStats = task {
    use client = new HttpClient()
    
    client.DefaultRequestHeaders.Referrer <- Uri("https://mysquadstats.com/")
    
    let! response = client.GetAsync(mySquadStatsUrlTemplate username)
    let! content = response.Content.ReadFromJsonAsync<MySquadStats>()
    
    return content.data
}

let playerStats = getPlayerStats |> Async.AwaitTask |> Async.RunSynchronously

let steamStatsUrlTemplate steamId = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={steamApiKey}&steamid={steamId}&format=json&&appids_filter[0]=393380"
type SteamInfo = { response: {| game_count: int; games: Option<seq<{| appid: int; playtime_2weeks: int; playtime_forever: int |}>> |} }

let steamClient = new HttpClient()
let getSteamUserGameStats steamId = task {
    let! response = steamClient.GetAsync(steamStatsUrlTemplate steamId)
    let! content = response.Content.ReadFromJsonAsync<SteamInfo>()
    
    return content
}

playerStats
    |> Seq.where (fun player -> findString (player.lastName.TrimStart()) username)
    |> Seq.map (fun player -> 
        let hours = 
            (getSteamUserGameStats player.steamID
            |> Async.AwaitTask
            |> Async.RunSynchronously).response.games
            |> function
                | Some games -> games |> Seq.tryLast |> Option.map (fun x -> x.playtime_forever / 60) |> Option.defaultValue 0
                | None -> 0
                
        {| Name = player.lastName.TrimStart()
           SteamID = player.steamID
           Hours = hours
           IsVisibleProfile = if hours = 0 then "Private" else "Visible" |}
    )
    |> Seq.sortByDescending (fun x -> x.Hours, x.Name.ToLower())
    |> fun players ->
        printfn "| #  | Name                | Steam Profile URL                                           | Hours    | Status   |"
        printfn "|----|---------------------|-------------------------------------------------------------|----------|----------|"
        players |> Seq.iteri (fun i player ->
            let statusColor = if player.IsVisibleProfile = "Private" then "\x1b[31m" else "\x1b[32m" // Red/Green
            printfn "| %-2d | %-19s | %-59s | %-8d | %s%-8s\x1b[0m |"
                (i + 1)
                player.Name
                ("https://steamcommunity.com/profiles/" + player.SteamID)
                player.Hours
                statusColor
                player.IsVisibleProfile
        )
