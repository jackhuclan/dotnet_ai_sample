"""第五轮验收：
1) 技能编辑窗口「系统提示词」文本框文字顶部左对齐
2) 聊天界面顶部区域的 avatar 已换成头像图形（不再是「我」字）

安全性：窗口按 MegaAgent.exe 的 PID 定位；点击用 PostMessage 投递到目标窗口
（不移动真实光标、不影响其它程序）；按钮操作用 UIA Invoke。
"""
import csv
import ctypes
import io
import os
import subprocess
import sys
import time

sys.path.insert(0, r"E:\dotnet_ai_sample\Mega.Agent\tests\MegaAgent.UiSmoke")

from PIL import ImageGrab
from pywinauto import Desktop

user32 = ctypes.windll.user32
WM_LBUTTONDOWN, WM_LBUTTONUP = 0x0201, 0x0202
SHOT_DIR = r"E:\dotnet_ai_sample\Mega.Agent\docs\screenshots"

# 关键：让本进程 DPI aware，使 GetWindowRect 返回物理像素坐标，
# 与 ImageGrab.grab() 的物理像素坐标系一致（否则截到错误区域/黑图）。
try:
    ctypes.windll.shcore.SetProcessDpiAwareness(2)
except Exception:
    user32.SetProcessDPIAware()


class POINT(ctypes.Structure):
    _fields_ = [("x", ctypes.c_long), ("y", ctypes.c_long)]


class RECT(ctypes.Structure):
    _fields_ = [("left", ctypes.c_long), ("top", ctypes.c_long),
                ("right", ctypes.c_long), ("bottom", ctypes.c_long)]


def megaagent_pids():
    out = subprocess.run(["tasklist", "/FI", "IMAGENAME eq MegaAgent.exe", "/FO", "CSV"],
                         capture_output=True).stdout.decode("gbk", errors="replace")
    return {int(r[1]) for r in csv.reader(io.StringIO(out))
            if len(r) >= 2 and r[0].lower() == "megaagent.exe"}


def hwnds_of(pids):
    res = []
    proc = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)

    def cb(h, lp):
        pid = ctypes.c_ulong()
        user32.GetWindowThreadProcessId(h, ctypes.byref(pid))
        if pid.value in pids and user32.GetWindowTextLengthW(h) > 0:
            res.append(h)
        return True

    user32.EnumWindows(proc(cb), None)
    return res


def post_click(hwnd, sx, sy):
    pt = POINT(sx, sy)
    user32.ScreenToClient(hwnd, ctypes.byref(pt))
    lparam = (pt.y << 16) | (pt.x & 0xFFFF)
    user32.PostMessageW(hwnd, WM_LBUTTONDOWN, 1, lparam)
    time.sleep(0.08)
    user32.PostMessageW(hwnd, WM_LBUTTONUP, 0, lparam)


def shot_rect(l, t, r, b, name):
    os.makedirs(SHOT_DIR, exist_ok=True)
    ImageGrab.grab(bbox=(l, t, r, b)).save(os.path.join(SHOT_DIR, name + ".png"))
    print("shot:", os.path.join(SHOT_DIR, name + ".png"))


def shot_hwnd(hwnd, name):
    r = RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(r))
    shot_rect(r.left, r.top, r.right, r.bottom, name)


def wins_now(pids):
    res = []
    proc = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)

    def cb(h, lp):
        pid = ctypes.c_ulong()
        user32.GetWindowThreadProcessId(h, ctypes.byref(pid))
        if pid.value in pids:
            cls = ctypes.create_unicode_buffer(256)
            user32.GetClassNameW(h, cls, 256)
            res.append((h, cls.value))
        return True

    user32.EnumWindows(proc(cb), None)
    return res


def find_window(pids, title):
    for w in Desktop(backend="uia").windows():
        try:
            if w.process_id() in pids and w.window_text() == title:
                return w
        except Exception:
            continue
    return None


def main():
    pids = megaagent_pids()
    if not pids:
        print("NO PROCESS")
        return
    hs = hwnds_of(pids)
    if not hs:
        print("NO WINDOW")
        return
    hwnd = hs[0]
    user32.SetForegroundWindow(hwnd)
    time.sleep(1.2)
    wr = RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(wr))

    # ---------- 1) 顶栏头像 ----------
    shot_rect(wr.right - 160, wr.top, wr.right, wr.top + 52, "50-topbar-avatar")
    win = None
    for _ in range(5):
        try:
            win = Desktop(backend="uia").window(handle=hwnd)
            win.descendants(control_type="Text")
            break
        except Exception as ex:
            print("UIA 连接重试:", ex)
            time.sleep(1.0)
    if win is None:
        print("UIA 连接失败")
        return
    print("顶栏所有文本:", [t.window_text() for t in win.descendants(control_type="Text")])
    print("是否还存在『我』字:", any(t.window_text().strip() == "我"
                                     for t in win.descendants(control_type="Text")))

    # ---------- 2) 技能编辑窗口的系统提示词输入框 ----------
    skills = find_window(pids, "技能管理")
    if skills is None:
        def popup_visible():
            for h2, cls, *_ in wins_now(pids):
                if cls.startswith("HwndWrapper") and h2 != hwnd:
                    r2 = RECT()
                    user32.GetWindowRect(h2, ctypes.byref(r2))
                    if user32.IsWindowVisible(h2) and (r2.bottom - r2.top) > 100:
                        return h2
            return None

        opened = False
        for attempt in range(2):
            post_click(hwnd, wr.right - 38, wr.top + 85)   # 头像中心（物理像素）
            time.sleep(1.6)
            if popup_visible():
                opened = True
                break
        print("头像菜单已打开:", opened)
        shot_hwnd(hwnd, "51-avatar-menu")
        if opened:
            for w in Desktop(backend="uia").windows():
                try:
                    if w.process_id() not in pids:
                        continue
                    for b in w.descendants(control_type="Button"):
                        if b.window_text() == "技能选择":
                            b.invoke()
                            break
                    else:
                        continue
                    break
                except Exception:
                    continue
        time.sleep(1.4)
        skills = find_window(pids, "技能管理")

    if skills is None:
        print("技能管理窗口未找到，跳过编辑器验证")
        return
    shot_hwnd(skills.handle, "52-skills-dialog")

    add = None
    for b in skills.descendants(control_type="Button"):
        if b.window_text() == "新建技能":
            add = b
            break
    if add is None:
        print("未找到『新建技能』按钮")
        return
    add.invoke()
    time.sleep(1.5)

    editor = find_window(pids, "编辑技能")
    if editor is None:
        print("编辑技能窗口未找到")
        return

    edits = editor.descendants(control_type="Edit")
    print("编辑窗口输入框数量:", len(edits))
    prompt_box = max(edits, key=lambda e: e.rectangle().height())
    print("系统提示词框 rect:", prompt_box.rectangle())

    # 填入多行文本，检查首行是否紧贴顶部
    prompt_box.set_edit_text("你是资深半导体产品经理。\r\n" + "\r\n".join(
        f"第 {i} 条规则：保持结构化、可落地的输出。" for i in range(1, 9)))
    time.sleep(0.8)
    shot_hwnd(editor.handle, "53-skill-editor-prompt-top")

    er = RECT()
    user32.GetWindowRect(editor.handle, ctypes.byref(er))
    pr = prompt_box.rectangle()
    # 截取文本框区域放大观察对齐
    shot_rect(pr.left - 4, pr.top - 26, min(pr.right + 4, er.right), pr.top + 90,
              "54-prompt-box-zoom")
    print("编辑窗口高度:", er.bottom - er.top, " 提示词框上边 y:", pr.top)


if __name__ == "__main__":
    main()
