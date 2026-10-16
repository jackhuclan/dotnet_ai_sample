"""第五轮验收（b）：通过头像菜单打开「技能管理 → 新建技能」，验证系统提示词框顶部对齐。"""
import ctypes
import csv
import io
import os
import subprocess
import sys
import time

sys.path.insert(0, r"E:\dotnet_ai_sample\Mega.Agent\tests\MegaAgent.UiSmoke")

from PIL import ImageGrab
from pywinauto import Desktop

user32 = ctypes.windll.user32
try:
    ctypes.windll.shcore.SetProcessDpiAwareness(2)
except Exception:
    user32.SetProcessDPIAware()

WM_LBUTTONDOWN, WM_LBUTTONUP = 0x0201, 0x0202
SHOT_DIR = r"E:\dotnet_ai_sample\Mega.Agent\docs\screenshots"


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


def wins_now(pids):
    res = []
    proc = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)

    def cb(h, lp):
        pid = ctypes.c_ulong()
        user32.GetWindowThreadProcessId(h, ctypes.byref(pid))
        if pid.value in pids:
            cls = ctypes.create_unicode_buffer(256)
            user32.GetClassNameW(h, cls, 256)
            r = RECT()
            user32.GetWindowRect(h, ctypes.byref(r))
            res.append((h, cls.value, r.left, r.top, r.right, r.bottom))
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


def shot_hwnd(hwnd, name):
    r = RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(r))
    os.makedirs(SHOT_DIR, exist_ok=True)
    p = os.path.join(SHOT_DIR, name + ".png")
    ImageGrab.grab(bbox=(r.left, r.top, r.right, r.bottom)).save(p)
    print("shot:", p)


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
    main_h = None
    for h, cls, l, t, r, b in wins_now(pids):
        if cls.startswith("HwndWrapper") and r - l > 400 and b - t > 600:
            main_h = h
    if main_h is None:
        print("NO MAIN WINDOW")
        return
    mr = RECT()
    user32.GetWindowRect(main_h, ctypes.byref(mr))
    user32.SetForegroundWindow(main_h)
    time.sleep(1.0)

    def popup_hwnd():
        for h, cls, l, t, r, b in wins_now(pids):
            if cls.startswith("HwndWrapper") and h != main_h and \
               user32.IsWindowVisible(h) and (b - t) > 100:
                return (h, l, t, r, b)
        return None

    # 若菜单已开着先点一下关掉，再点开
    if popup_hwnd():
        post_click(main_h, mr.right - 38, mr.top + 85)
        time.sleep(1.0)
    post_click(main_h, mr.right - 38, mr.top + 85)
    time.sleep(1.6)

    ph = popup_hwnd()
    if not ph:
        print("菜单未打开")
        return
    h2, pl, pt_, pr2, pb = ph
    print("popup rect:", pl, pt_, pr2, pb)
    shot_hwnd(h2, "55-avatar-popup")

    # 「技能选择」菜单项：位于卡片下半部，取弹层中部偏下
    post_click(h2, (pl + pr2) // 2, pb - 90)
    time.sleep(1.8)

    skills = find_window(pids, "技能管理")
    if skills is None:
        print("技能管理未打开")
        return
    shot_hwnd(skills.handle, "56-skills-dialog")

    for b in skills.descendants(control_type="Button"):
        if b.window_text() == "新建技能":
            b.invoke()
            break
    time.sleep(1.8)

    editor = find_window(pids, "编辑技能")
    if editor is None:
        print("编辑技能窗口未找到")
        return

    edits = editor.descendants(control_type="Edit")
    print("输入框数量:", len(edits))
    prompt_box = max(edits, key=lambda e: e.rectangle().height())
    prompt_box.set_edit_text("你是资深半导体产品经理。\r\n" + "\r\n".join(
        f"第 {i} 条规则：保持结构化、可落地的输出。" for i in range(1, 9)))
    time.sleep(0.8)
    shot_hwnd(editor.handle, "57-skill-editor-top-aligned")
    print("验证完成")


if __name__ == "__main__":
    main()
