"""验证新版布局：默认隐藏抽屉 + 展开覆盖聊天区 50% + 头像在右侧顶栏。

全程使用 UIA 的 Invoke 模式（不移动鼠标、不抢占焦点以外的操作），
并且只截取 MegaAgent 窗口区域，避免干扰用户其它工作。
"""
import ctypes
import sys
import time

sys.path.insert(0, r"E:\dotnet_ai_sample\Mega.Agent\tests\MegaAgent.UiSmoke")

from PIL import ImageGrab
from pywinauto import Desktop

user32 = ctypes.windll.user32


class RECT(ctypes.Structure):
    _fields_ = [("left", ctypes.c_long), ("top", ctypes.c_long),
                ("right", ctypes.c_long), ("bottom", ctypes.c_long)]


def window_rect(hwnd):
    r = RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(r))
    return r.left, r.top, r.right, r.bottom


def shot_window(hwnd, name):
    l, t, r, b = window_rect(hwnd)
    ImageGrab.grab(bbox=(l, t, r, b)).save(
        rf"E:\dotnet_ai_sample\Mega.Agent\shots\{name}.png")
    print("shot:", name, f"size={r - l}x{b - t}")


def main():
    hs = [h for h in _find("MegaAgent")]
    if not hs:
        print("NO WINDOW")
        return
    hwnd = hs[0]
    user32.ShowWindow(hwnd, 9)
    user32.SetForegroundWindow(hwnd)
    time.sleep(1.0)

    win = Desktop(backend="uia").window(handle=hwnd)
    l, t, r, b = window_rect(hwnd)
    w, h = r - l, b - t
    print(f"window: {w}x{h}  ratio(w/h)={w / h:.4f} (9:16={9 / 16:.4f})")

    # 1) 默认状态：抽屉隐藏，仅聊天窗口
    shot_window(hwnd, "10-default-drawer-hidden")
    texts = [x.window_text() for x in win.descendants(control_type="Text")]
    print("抽屉内容可见:", any("历史对话" in x for x in texts))

    # 2) 点击 ☰ 展开抽屉（UIA Invoke，不移动鼠标）
    toggle = [b for b in win.descendants(control_type="Button")
              if b.window_text() == "☰"][0]
    toggle.invoke()
    time.sleep(1.0)
    shot_window(hwnd, "11-drawer-open")

    # 抽屉宽度 = 50% 聊天区宽度
    drawer = [x for x in win.descendants(control_type="Text")
              if x.window_text() == "历史对话"]
    print("历史对话标题可见:", bool(drawer))
    if drawer:
        dr = drawer[0].rectangle()
        print(f"抽屉内文本 left={dr.left - l} (应 < 50%*{w}={w // 2})")

    # 3) 头像位于右侧顶栏
    avatars = [b for b in win.descendants(control_type="Button")
               if b.window_text() == "" and b.rectangle().height() < 45
               and b.rectangle().left - l > w * 0.7]
    print("右侧顶栏头像按钮:", [(a.rectangle().left - l, a.rectangle().top - t)
                                for a in avatars])

    # 4) 打开头像菜单（Invoke）
    if avatars:
        avatars[0].invoke()
        time.sleep(0.8)
        shot_window(hwnd, "12-avatar-menu")
        menu_texts = [x.window_text() for x in win.descendants(control_type="Text")]
        print("菜单项可见:", [m for m in menu_texts if "模型设置" in m or "技能" in m])
        for b in win.descendants(control_type="Button"):
            if b.window_text() == "模型设置":
                b.invoke()
                time.sleep(1.2)
                break
        dialogs = _find("模型设置")
        print("设置弹窗已打开:", bool(dialogs))
        if dialogs:
            dlg = Desktop(backend="uia").window(handle=dialogs[0])
            for b in dlg.descendants(control_type="Button"):
                if b.window_text() == "取消":
                    b.invoke()
                    break
            time.sleep(0.6)

    # 5) 关闭抽屉
    close = [b for b in win.descendants(control_type="Button")
             if b.window_text() == "«"]
    if close:
        close[0].invoke()
        time.sleep(0.9)
    shot_window(hwnd, "13-drawer-closed")


def _find(sub):
    res = []
    proc = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)

    def cb(h, lp):
        n = user32.GetWindowTextLengthW(h)
        if n:
            buf = ctypes.create_unicode_buffer(n + 1)
            user32.GetWindowTextW(h, buf, n + 1)
            if sub in buf.value:
                res.append(h)
        return True

    user32.EnumWindows(proc(cb), None)
    return res


if __name__ == "__main__":
    main()
