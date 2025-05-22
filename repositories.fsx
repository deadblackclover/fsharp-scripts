open System
open System.Net.Http
open System.Net.Http.Json

type Repository = { name: string; clone_url: string }

type Service =
    | GitHub
    | Codeberg

let createURL username limit service =
    match service with
    | GitHub -> $"https://api.github.com/users/{username}/repos?per_page={limit}"
    | Codeberg -> $"https://codeberg.org/api/v1/users/{username}/repos?limit={limit}"

let createRequest (url: string) =
    let request = new HttpRequestMessage(HttpMethod.Get, url)

    request.Headers.Add("Accept", "application/json")
    request.Headers.Add("User-Agent", "curl/7.87.0")

    request

let getRepositories username limit service =
    let request = createURL username limit service |> createRequest

    task {
        use client = new HttpClient()

        let! response = client.SendAsync(request)

        if response.IsSuccessStatusCode then
            let! repositories = response.Content.ReadFromJsonAsync<Repository Set>()

            return Set.map (fun repository -> repository.name) repositories
        else
            printfn "Response status code: %d" ((int) response.StatusCode)
            return Set.empty
    }
    |> Async.AwaitTask
    |> Async.RunSynchronously

let getListChanges left right username limit =
    let x = getRepositories username limit
    let repositories = x right
    let result = Set.intersect (x left) repositories

    repositories
    |> Set.filter (fun repository -> not (Set.contains repository result))

let args = Environment.GetCommandLineArgs()
let limit = 100

for item in getListChanges GitHub Codeberg args[2] limit do
    printfn "%s" item
