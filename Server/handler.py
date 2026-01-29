import json
from protocol import make_response, ERROR_CODES
from room import room_manager
from player import player_manager
from server_status import log_server_event
import threading
import time

player_count = 0
player_lock = threading.Lock()

# 최소 요청 간격(서버 측 과다 호출 탐지용)
LIST_MIN_INTERVAL_SEC = 1.0
HEARTBEAT_MIN_INTERVAL_SEC = 1.0

# 클라이언트 요청 처리

def handle_client(conn, addr):
    global player_count
    deviceId = None
    nickname = None
    disconnect_reason = "정상 종료"
    start_time = time.time()
    req_seq = 0
    last_list_ts = 0.0
    last_heartbeat_ts = 0.0
    try:
        buffer = ""
        while True:
            raw = conn.recv(4096)
            if not raw:
                disconnect_reason = "클라이언트가 연결을 닫음 (recv=0)"
                break
            try:
                buffer += raw.decode()
            except Exception:
                # 디코드 실패 시 로그만 남기고 다음 루프로
                log_server_event(f"[WARN] 디코드 실패: addr={addr}, raw_size={len(raw)}")
                continue

            # 개행 구분자 기준으로 메시지 분리
            while "\n" in buffer:
                line, buffer = buffer.split("\n", 1)
                if not line.strip():
                    continue
                try:
                    msg = json.loads(line)
                except Exception:
                    log_server_event(f"[WARN] JSON 파싱 오류: addr={addr}, line_size={len(line)}")
                    conn.sendall((make_response(False, error={"code": ERROR_CODES["JSON_PARSE"], "message": "JSON 파싱 오류"})+"\n").encode())
                    continue

                msg_type = msg.get("type")
                deviceId = msg.get("deviceId")
                nickname = msg.get("nickname", "Unknown")
                sessionToken = msg.get("sessionToken")

            # 메시지 처리 전 공통 로깅(요청 수신)
            if 'msg_type' in locals() and msg_type:
                req_seq += 1
                log_server_event(f"[SOCKET {addr}] [요청] seq={req_seq}, type={msg_type}, by={deviceId}")

            # 인증 및 중복 로그인 방지
            if msg_type == "auth":
                player = player_manager.add_player(conn, addr, deviceId, nickname)
                if not player:
                    log_server_event(f"[WARN] 중복 로그인 시도: addr={addr}, deviceId={deviceId}")
                    conn.sendall((make_response(False, error={"code": ERROR_CODES["DUPLICATE"], "message": "중복 로그인"})+"\n").encode())
                else:
                    log_server_event(f"[AUTH OK] addr={addr}, deviceId={deviceId}, nick={nickname}")
                    resp = {"type": "auth", "sessionToken": player.sessionToken, "serverSeq": req_seq}
                    if msg.get("clientSeq") is not None:
                        resp["clientSeq"] = msg.get("clientSeq")
                    conn.sendall((make_response(True, data=resp)+"\n").encode())
                continue

            # 세션 체크 (실제 서비스 시 필수)
            player = player_manager.get_player(deviceId)
            if not player or player.sessionToken != sessionToken:
                log_server_event(f"[WARN] 인증 실패: addr={addr}, deviceId={deviceId}, hasPlayer={bool(player)}, tokenMatched={player and (player.sessionToken == sessionToken)}")
                conn.sendall((make_response(False, error={"code": ERROR_CODES["UNAUTHORIZED"], "message": "인증 실패"})+"\n").encode())
                continue

            if msg_type == "create":
                room, err = room_manager.create_room(msg)
                if room:
                    room_manager.room_players[room.roomId].add(deviceId)
                    room.currentPlayers = 1
                    data = {"type": "create", "roomId": room.roomId, "sessionToken": player.sessionToken, "data": room.to_dict(), "serverSeq": req_seq}
                    if msg.get("clientSeq") is not None:
                        data["clientSeq"] = msg.get("clientSeq")
                    log_server_event(f"[SOCKET {addr}] [방 생성] roomId={room.roomId}, host={room.hostAddress}:{room.hostPort}, name={room.roomName}, by={deviceId}")
                    conn.sendall((make_response(True, data=data)+"\n").encode())
                else:
                    log_server_event(f"[CREATE FAIL] addr={addr}, by={deviceId}, reason={err}, payload_keys={list(msg.keys())}")
                    conn.sendall((make_response(False, error={"code": ERROR_CODES["FORBIDDEN"], "message": err})+"\n").encode())

            elif msg_type == "list":
                include_private = msg.get("includePrivate", False)
                now = time.time()
                if last_list_ts > 0 and (now - last_list_ts) < LIST_MIN_INTERVAL_SEC:
                    log_server_event(f"[WARN] 방 목록 요청 과다: interval={now - last_list_ts:.2f}s, by={deviceId}, addr={addr}")
                last_list_ts = now
                rooms = room_manager.get_room_list(include_private)
                log_server_event(f"방 목록 요청: by={deviceId}, 반환 방 수={len(rooms)}")
                resp = {"type": "list", "rooms": rooms, "serverSeq": req_seq}
                if msg.get("clientSeq") is not None:
                    resp["clientSeq"] = msg.get("clientSeq")
                conn.sendall((make_response(True, data=resp)+"\n").encode())

            elif msg_type == "join":
                roomId = msg.get("roomId")
                room, err = room_manager.join_room(roomId, deviceId)
                if room:
                    data = {"type": "join", "hostAddress": room.hostAddress, "hostPort": room.hostPort, "roomInfo": room.to_dict(), "joinCode": room.joinCode, "serverSeq": req_seq}
                    if msg.get("clientSeq") is not None:
                        data["clientSeq"] = msg.get("clientSeq")
                    log_server_event(f"[SOCKET {addr}] [방 참가] roomId={roomId}, by={deviceId}")
                    conn.sendall((make_response(True, data=data)+"\n").encode())
                else:
                    conn.sendall((make_response(False, error={"code": ERROR_CODES["ROOM_FULL"], "message": err})+"\n").encode())

            elif msg_type == "leave":
                roomId = msg.get("roomId")
                ok, err = room_manager.leave_room(roomId, deviceId)
                if ok:
                    log_server_event(f"방 퇴장: roomId={roomId}, by={deviceId}")
                    resp = {"type": "leave", "roomId": roomId, "serverSeq": req_seq}
                    if msg.get("clientSeq") is not None:
                        resp["clientSeq"] = msg.get("clientSeq")
                    conn.sendall((make_response(True, data=resp)+"\n").encode())
                else:
                    conn.sendall((make_response(False, error={"code": ERROR_CODES["NOT_FOUND"], "message": err})+"\n").encode())

            elif msg_type == "heartbeat":
                roomId = msg.get("roomId")
                if roomId in room_manager.rooms:
                    now = time.time()
                    if last_heartbeat_ts > 0 and (now - last_heartbeat_ts) < HEARTBEAT_MIN_INTERVAL_SEC:
                        log_server_event(f"[WARN] 하트비트 과다: interval={now - last_heartbeat_ts:.2f}s, roomId={roomId}, by={deviceId}, addr={addr}")
                    last_heartbeat_ts = now
                    room_manager.rooms[roomId].lastHeartbeat = int(now)
                    log_server_event(f"하트비트: roomId={roomId}, by={deviceId}")
                    resp = {"type": "heartbeat", "roomId": roomId, "serverSeq": req_seq}
                    if msg.get("clientSeq") is not None:
                        resp["clientSeq"] = msg.get("clientSeq")
                    conn.sendall((make_response(True, data=resp)+"\n").encode())
                else:
                    conn.sendall((make_response(False, error={"code": ERROR_CODES["NOT_FOUND"], "message": "방이 존재하지 않습니다"})+"\n").encode())

            # 플레이어 목록 요청: 'playerList' 또는 'getPlayerList' 모두 지원
            elif msg_type == "playerList" or msg_type == "getPlayerList":
                roomId = msg.get("roomId")
                player_list = room_manager.get_player_list(roomId, player_manager)
                resp = {"type": "playerList", "players": player_list, "serverSeq": req_seq}
                if msg.get("clientSeq") is not None:
                    resp["clientSeq"] = msg.get("clientSeq")
                conn.sendall((make_response(True, data=resp)+"\n").encode())

            # 방 삭제 요청: 방장만 가능, sessionToken 검증
            elif msg_type == "delete":
                roomId = msg.get("roomId")
                token = msg.get("sessionToken")
                room = room_manager.rooms.get(roomId)
                if not room:
                    conn.sendall((make_response(False, error={"code": ERROR_CODES["NOT_FOUND"], "message": "방이 존재하지 않습니다"})+"\n").encode())
                    continue
                if room.hostDeviceId != deviceId:
                    conn.sendall((make_response(False, error={"code": ERROR_CODES["FORBIDDEN"], "message": "방장만 삭제할 수 있습니다"})+"\n").encode())
                    continue
                host_player = player_manager.get_player(deviceId)
                if not host_player or host_player.sessionToken != token:
                    conn.sendall((make_response(False, error={"code": ERROR_CODES["UNAUTHORIZED"], "message": "세션 토큰 불일치"})+"\n").encode())
                    continue
                # 방 삭제
                del room_manager.rooms[roomId]
                del room_manager.room_players[roomId]
                log_server_event(f"[SOCKET {addr}] [방 삭제] roomId={roomId}, by={deviceId}")
                resp = {"type": "delete", "roomId": roomId, "serverSeq": req_seq}
                if msg.get("clientSeq") is not None:
                    resp["clientSeq"] = msg.get("clientSeq")
                conn.sendall((make_response(True, data=resp)+"\n").encode())

            else:
                conn.sendall((make_response(False, error={"code": ERROR_CODES["NOT_FOUND"], "message": "알 수 없는 요청 타입"})+"\n").encode())
    except Exception as e:
        disconnect_reason = f"서버 예외 발생: {str(e)}"
        print(f"Error: {e}")
        try:
            # 예외 발생 시에도 서버 로그에 기록
            log_server_event(f"[ERROR] {str(e)}")
            # 예외 발생 시에도 클라이언트에 에러 응답 전송
            conn.sendall((make_response(False, error={"code": 500, "message": f"서버 내부 오류: {str(e)}"})+"\n").encode())
        except Exception as send_ex:
            print(f"Error sending error response: {send_ex}")
            log_server_event(f"[ERROR] Error sending error response: {str(send_ex)}")
    finally:
        # 연결 종료 시 해당 deviceId가 속한 모든 방에서 leave_room 호출
        if deviceId:
            rooms_to_leave = []
            for roomId, players in list(room_manager.room_players.items()):
                if deviceId in players:
                    rooms_to_leave.append(roomId)
            for roomId in rooms_to_leave:
                room_manager.leave_room(roomId, deviceId)
        duration = time.time() - start_time
        log_server_event(f"[연결 종료] addr={addr}, deviceId={deviceId}, reason={disconnect_reason}, reqCount={req_seq}, duration={duration:.2f}s")
        conn.close()
        with player_lock:
            if player_count > 0:
                player_count -= 1
        player_manager.remove_player(deviceId) 