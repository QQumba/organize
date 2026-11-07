# Core is a separate HTTP service; Clients are thin and have no offline mode

The Core owns all data and rules and exposes an HTTP/JSON API with token authentication; the CLI, desktop app and web UI are thin Clients with no logic or storage of their own. This lets the Core move to a remote server later without changing any Client. We rejected local-only IPC and per-client storage because they would block remote hosting, and rejected offline queueing and sync because of the scope they add. A Client that cannot reach the Core shows a clear error.
