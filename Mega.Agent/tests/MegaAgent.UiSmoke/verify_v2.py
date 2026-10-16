"""第二轮验收：固定窗口尺寸 / 抽屉无品牌区 / 头像完整 / 斜杠列出全部技能。"""
import ctypes
import sys
import time

sys.path.insert(0, r"E:\dotnet_ai_sample\Mega.Agent\tests\MegaAgent.UiSmoke")

from PIL import ImageGrab
from pywinauto import Desktop

user32 = ctypes.windll.user32
GWL_STYLE = -16
WS_THICKFRAME = 0x00040000
WS_MAXIMIZEBOX = 0x00010000


class RECT(ctypes.Structure):
    _fields_ = [("left", ctypes.c_long), ("top", ctypes.c_long),
                ("right", ctypes.c_long), ("bottom", ctypes.c_long)]


def find_hwnds(sub):
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


def shot(hwnd, name):
    r = RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(r))
    ImageGrab.grab(bbox=(r.left, r.top, r.right, r.bottom)).save(
        rf"E:\dotnet_ai_sample\Mega.Agent\shots\{name}.png")
    print("shot:", name)


def main():
    hs = find_hwnds("MegaAgent")
    if not hs:
        print("NO WINDOW")
        return
    hwnd = hs[0]
    user32.ShowWindow(hwnd, 9)
    user32.SetForegroundWindow(hwnd)
    time.sleep(1.0)

    # 1) 固定尺寸：检查窗口样式位
    style = user32.GetWindowLongW(hwnd, GWL_STYLE)
    print(f"1) WS_THICKFRAME(可拖拽调整大小)={bool(style & WS_THICKFRAME)} "
          f"WS_MAXIMIZEBOX(可最大化)={bool(style & WS_MAXIMIZEBOX)}")

    win = Desktop(backend="uia").window(handle=hwnd)
    shot(hwnd, "20-fixed-avatar")

    # 2) 抽屉内不应再有 MegaAgent 品牌区
    toggle = [b for b in win.descendants(control_type="Button")
              if b.window_text() == "☰"][0]
    toggle.invoke()
    time.sleep(1.0)
    shot(hwnd, "21-drawer-no-brand")
    drawer_texts = [t.window_text() for t in win.descendants(control_type="Text")
                    if t.rectangle().left - win.rectangle().left < 300]
    print("2) 抽屉内文本:", [t for t in drawer_texts if t.strip()][:6])
    print("   品牌区 MegaAgent 是否仍在:", "MegaAgent" in drawer_texts)

    close = [b for b in win.descendants(control_type="Button")
             if b.window_text() == "«"]
    if close:
        close[0].invoke()
        time.sleep(0.9)

    # 3) 斜杠技能：输入 "/" 应列出全部技能
    win = Desktop(backend="uia").window(handle=hwnd)
    edits = win.descendants(control_type="Edit")
    print("3) 输入框数量:", len(edits))
    box = edits[0]
    box.set_edit_text("/")           # UIA ValuePattern，不移动鼠标
    time.sleep(1.0)
    shot(hwnd, "22-slash-all-skills")

    popup_items = [t.window_text() for t in win.descendants(control_type="Text")
                   if t.window_text().startswith("选择技能")]
    print("   浮层标题:", popup_items)

    # 技能名来自技能库
    import json
    skills = json.load(open(r"C:\Users\Megarobo\AppData\Roaming\MegaAgent\skills.json",
                            encoding="utf-8"))
    names = [s["Name"] for s in skills]
    print("   技能库:", names)

    # 选中第一项（点按钮的 UIA Invoke，不移动鼠标）
    item_btns = [b for b in win.descendants(control_type="Button")
                 if b.window_text() in names]
    print("   浮层内可见技能按钮:", [b.window_text() for b in item_btns])
    if item_btns:
        item_btns[0].invoke()
        time.sleep(0.8)
    shot(hwnd, "23-skill-selected")
    chip = [t.window_text() for t in win.descendants(control_type="Text")
            if t.window_text() in names]
    print("   选中后输入区技能 chip:", chip)

    # 4) 发送链路（Text 绑定修复后应能真实调用）
    box = Desktop(backend="uia").window(handle=hwnd).descendants(control_type="Edit")[0]
    box.set_edit_text("用一句话说明你收到了这条消息")
    time.sleep(0.5)
    send = [b for b in win.descendants(control_type="Button")
            if b.window_text() == "发送"][0]
    send.invoke()
    print("4) 已发送，等待流式回复…")
    for i in range(30):
        time.sleep(3)
        texts = [t.window_text() for t in win.descendants(control_type="Text")]
        if i > 0 and not any("生成中" in t for t in texts):
            print(f"   完成 ~{(i + 1) * 3}s")
            break
    shot(hwnd, "24-sent-reply")


if __name__ == "__main__":
    main()
