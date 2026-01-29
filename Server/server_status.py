import datetime
import os
import json

def print_server_status(config):
    print("==============================")
    print("   Python Lobby Server Start   ")
    print("==============================")
    print(f"서버 IP      : {config.get('SERVER_IP', '0.0.0.0')}")
    print(f"서버 포트    : {config.get('SERVER_PORT', 9000)}")
    print(f"최대 인원    : {config.get('MAX_PLAYERS', 1000)}")
    print(f"최대 방 수   : {config.get('MAX_ROOMS', 100)}")
    print("==============================\n")

def log_server_event(message, log_file="server.log"):
    now = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    log_line = f"[{now}] {message}\n"
    with open(log_file, "a", encoding="utf-8") as f:
        f.write(log_line)

# 실시간 상태 표기 함수
# room_manager, player_manager를 인자로 받아야 함

def print_live_status(room_manager, player_manager):
    print("\n===== [서버 실시간 상태] =====")
    rooms = room_manager.rooms
    print(f"총 방 수: {len(rooms)}")
    print(f"rooms dict 내용: {rooms}")  # 디버깅용 실제 딕셔너리 내용 출력
    if not rooms:
        print("현재 생성된 방이 없습니다.")
    else:
        for idx, (roomId, room) in enumerate(rooms.items(), 1):
            players = room_manager.room_players.get(roomId, set())
            print(f"\n[{idx}] 방ID: {roomId}")
            print(f"  방 이름      : {getattr(room, 'roomName', '-')}")
            print(f"  호스트 IP    : {getattr(room, 'hostAddress', '-')}:{getattr(room, 'hostPort', '-')}")
            print(f"  인원         : {len(players)}/{getattr(room, 'maxPlayers', '-')}")
            print(f"  방장         : {getattr(room, 'hostDeviceId', '-')}")
            print(f"  생성시각     : {datetime.datetime.fromtimestamp(getattr(room, 'createdTime', 0)).strftime('%Y-%m-%d %H:%M:%S')}")
            print(f"  상태         : {getattr(room, 'status', '공개')}")
            print(f"  게임 타입    : {getattr(room, 'gameType', '-')}")
            print(f"  플레이어 목록:")
            for deviceId in players:
                player = player_manager.get_player(deviceId)
                if player:
                    print(f"    - {player.nickname} (ID: {player.deviceId})"
                          f"{' [방장]' if player.deviceId == room.hostDeviceId else ''}"
                          f"{' [준비]' if getattr(player, 'isReady', False) else ''}"
                          f" 입장: {datetime.datetime.fromtimestamp(getattr(player, 'joinTime', 0)).strftime('%H:%M:%S')}")
    print("============================\n") 