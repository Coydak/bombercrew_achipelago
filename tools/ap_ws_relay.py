"""
Local plain-ws <-> remote wss relay for the Bomber Crew Archipelago mod.

Bomber Crew runs on an old Unity/Mono runtime that can't complete a modern TLS handshake, so
it can't connect directly to archipelago.gg's TLS-secured hosted rooms (local self-hosted
servers work fine because they're plain ws://, unencrypted). This script sits in between: the
mod connects to it locally over plain ws:// exactly like it always has, and this script (a
normal Python process with a real TLS stack) does the actual wss:// handshake to the remote
room and transparently relays every message both ways. No router/port-forwarding changes
needed - only outbound connections are made from this machine.

Usage:
    python tools/ap_ws_relay.py --remote archipelago.gg:33749 [--local-port 39000]

Then connect the mod to `localhost:39000` (or whatever --local-port you chose) instead of the
remote address directly.
"""

import argparse
import asyncio
import logging

import websockets

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
log = logging.getLogger("ap_ws_relay")


async def pump(source, destination, label):
    try:
        async for message in source:
            await destination.send(message)
    except websockets.exceptions.ConnectionClosed:
        pass
    finally:
        await destination.close()
        log.info("%s side closed", label)


async def handle_local_connection(local_ws, remote_uri):
    log.info("Local client connected, dialing %s ...", remote_uri)
    try:
        async with websockets.connect(remote_uri) as remote_ws:
            log.info("Connected to %s", remote_uri)
            await asyncio.gather(
                pump(local_ws, remote_ws, "local->remote"),
                pump(remote_ws, local_ws, "remote->local"),
            )
    except Exception as ex:
        log.error("Failed to relay to %s: %s", remote_uri, ex)
        await local_ws.close()


async def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--remote", required=True, help="Remote host:port, e.g. archipelago.gg:33749")
    parser.add_argument("--local-port", type=int, default=39000, help="Local port to listen on (default 39000)")
    args = parser.parse_args()

    remote_uri = args.remote
    if not remote_uri.startswith("ws://") and not remote_uri.startswith("wss://"):
        remote_uri = f"wss://{remote_uri}"

    async def handler(local_ws):
        await handle_local_connection(local_ws, remote_uri)

    async with websockets.serve(handler, "localhost", args.local_port):
        log.info("Listening on ws://localhost:%d -> relaying to %s", args.local_port, remote_uri)
        log.info("Point the Bomber Crew mod's Host field at: localhost:%d", args.local_port)
        await asyncio.Future()  # run forever


if __name__ == "__main__":
    asyncio.run(main())
