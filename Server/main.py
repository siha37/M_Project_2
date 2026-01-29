import socket
import threading
from handler import handle_client
from room import room_manager
from player import player_manager
import time
import json
import os
from server_status import print_server_status, log_server_event, print_live_status

# config.json에서 설정 읽기
def load_config():
    config_path = os.path.join(os.path.dirname(__file__), "config.json")
    with open(config_path, "r", encoding="utf-8") as f:
        return json.load(f)

config = load_config()
SERVER_IP = config.get("SERVER_IP", "0.0.0.0")
SERVER_PORT = config.get("SERVER_PORT", 9000)
MAX_PLAYERS = config.get("MAX_PLAYERS", 1000)
MAX_ROOMS = config.get("MAX_ROOMS", 100)


def heartbeat_monitor():
    while True:
        now = time.time()
        for room in list(room_manager.rooms.values()):
            if now - room.lastHeartbeat > 60:
                from server_status import log_server_event
                log_server_event(f"[WARN] 하트비트 타임아웃: roomId={room.roomId}, host={room.hostDeviceId}, lastHeartbeatAgo={int(now - room.lastHeartbeat)}s")
                room_manager.leave_room(room.roomId, room.hostDeviceId)
        time.sleep(10)

# 입력 대기 스레드: ctrl+l 입력 시 상태 출력
def input_monitor():
    import sys
    import msvcrt
    print("[INFO] 서버 상태를 보려면 Ctrl+L을 누르세요.")
    while True:
        if msvcrt.kbhit():
            key = msvcrt.getch()
            if key == b'\x0c':  # Ctrl+L
                print_live_status(room_manager, player_manager)
        time.sleep(0.1)

def main():
    print_server_status(config)
    log_server_event(f"서버 시작: {SERVER_IP}:{SERVER_PORT}")
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.bind((SERVER_IP, SERVER_PORT))
    server.listen(MAX_PLAYERS)
    print("서버 시작: {}:{}".format(SERVER_IP, SERVER_PORT))
    threading.Thread(target=heartbeat_monitor, daemon=True).start()
    threading.Thread(target=input_monitor, daemon=True).start()
    try:
        while True:
            conn, addr = server.accept()
            log_server_event(f"클라이언트 접속: {addr}")
            threading.Thread(target=handle_client, args=(conn, addr), daemon=True).start()
    except KeyboardInterrupt:
        print("서버 종료")
        log_server_event("서버 종료")
    finally:
        server.close()

if __name__ == "__main__":
    main() 