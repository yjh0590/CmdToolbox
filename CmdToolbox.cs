// 명령어 도구함 - cmd에서 직접 치던 명령어들을 버튼으로 모아둔 앱
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// ───────────────────────── 테마 ─────────────────────────
static class Theme
{
    public static readonly Color Sidebar = Color.FromArgb(16, 16, 22);
    public static readonly Color Bg = Color.FromArgb(22, 22, 30);
    public static readonly Color Surface = Color.FromArgb(34, 34, 48);
    public static readonly Color SurfaceHover = Color.FromArgb(48, 48, 66);
    public static readonly Color Text = Color.FromArgb(232, 232, 240);
    public static readonly Color Dim = Color.FromArgb(138, 138, 160);
    public static readonly Color Accent = Color.FromArgb(255, 122, 89);
    public static readonly Color AccentHover = Color.FromArgb(255, 145, 116);

    public static readonly Font Body = new Font("Malgun Gothic", 10f);
    public static readonly Font Mono = new Font("Consolas", 9.5f);
    public static readonly Font MonoSmall = new Font("Consolas", 8.5f);

    public static GraphicsPath Round(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public const TextFormatFlags Center = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                          TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr hwnd, string app, string idList);

    // 스크롤바 등을 다크 모드로
    public static void Dark(Control c)
    {
        c.HandleCreated += (s, e) => { try { SetWindowTheme(c.Handle, "DarkMode_Explorer", null); } catch { } };
    }
}

// ───────────────────────── 공통 컨트롤 ─────────────────────────
abstract class PaintedControl : Control
{
    protected PaintedControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        ForeColor = Theme.Text;
        BackColor = Theme.Bg;
        Font = Theme.Body;
    }
}

// 둥근 버튼 (Sub가 있으면 두 줄 타일)
class RoundButton : PaintedControl
{
    public Color Fill = Theme.Surface, HoverFill = Theme.SurfaceHover, Border = Color.Empty;
    public float Radius = 10;
    public string Sub = "";
    bool hover, down;

    public RoundButton() { Cursor = Cursors.Hand; }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        if (down) r.Inflate(-1, -1);
        Color fill = !Enabled ? Theme.Surface : hover ? HoverFill : Fill;
        Color fore = Enabled ? ForeColor : Theme.Dim;
        using (var path = Theme.Round(r, Radius))
        {
            using (var b = new SolidBrush(fill)) g.FillPath(b, path);
            if (Border != Color.Empty)
                using (var pen = new Pen(hover && Enabled ? Theme.Dim : Border, 1.5f)) g.DrawPath(pen, path);
        }
        if (Sub == "")
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, fore, Theme.Center);
        else
        {
            TextRenderer.DrawText(g, Text, Font, new Rectangle(6, Height / 2 - 21, Width - 12, 22), fore, Theme.Center);
            TextRenderer.DrawText(g, Sub, Theme.MonoSmall, new Rectangle(6, Height / 2 + 2, Width - 12, 18), Theme.Dim, Theme.Center);
        }
    }
}

// 사이드바 목록 항목
class NavItem : PaintedControl
{
    public int Number;
    public bool Selected;
    bool hover;

    public NavItem() { Cursor = Cursors.Hand; BackColor = Theme.Sidebar; }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (Selected || hover)
            using (var path = Theme.Round(new RectangleF(0, 0, Width - 1, Height - 1), 8))
            using (var b = new SolidBrush(Selected ? Theme.Surface : Color.FromArgb(26, 26, 36)))
                g.FillPath(b, path);
        if (Selected)
            using (var path = Theme.Round(new RectangleF(0, 9, 4, Height - 18), 2))
            using (var b = new SolidBrush(Theme.Accent))
                g.FillPath(b, path);

        TextRenderer.DrawText(g, Number.ToString(), Theme.Mono, new Rectangle(12, 0, 24, Height),
            Selected ? Theme.Accent : Theme.Dim, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(46, 0, Width - 50, Height),
            Selected ? Theme.Text : Color.FromArgb(190, 190, 205), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

// 둥근 카드 배경
class Card : Panel
{
    public Card()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Surface;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.Bg);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = Theme.Round(new RectangleF(0, 0, Width - 1, Height - 1), 12))
        using (var b = new SolidBrush(Theme.Surface))
            g.FillPath(b, path);
    }
}

// 남은 시간 원형 표시
class Ring : PaintedControl
{
    public float Progress;
    public bool Active;
    public string Big = "", Small = "";
    readonly Font bigFont = new Font("Segoe UI Light", 32f);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new RectangleF(10, 10, Width - 20, Height - 20);
        using (var track = new Pen(Theme.Surface, 12)) g.DrawEllipse(track, r);
        if (Active && Progress > 0)
            using (var arc = new Pen(Theme.Accent, 12) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(arc, r, -90, 360 * Progress);

        TextRenderer.DrawText(g, Big, bigFont, new Rectangle(0, Height / 2 - 38, Width, 54),
            Active ? Theme.Text : Theme.Dim, Theme.Center);
        TextRenderer.DrawText(g, Small, Font, new Rectangle(0, Height / 2 + 18, Width, 24),
            Active ? Theme.Accent : Theme.Dim, Theme.Center);
    }
}

// ───────────────────────── 명령 실행 도우미 ─────────────────────────
static class Shell
{
    // 콘솔 프로그램 출력 인코딩 (한국어 Windows는 CP949)
    static readonly Encoding Oem = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);

    static ProcessStartInfo Hidden(string file, string args)
    {
        return new ProcessStartInfo(file, args)
        {
            CreateNoWindow = true, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Oem, StandardErrorEncoding = Oem
        };
    }

    // 창 없이 실행하고 출력 결과를 문자열로 받기
    public static Task<string> Capture(string file, string args)
    {
        return Task.Run(() =>
        {
            using (var p = Process.Start(Hidden(file, args)))
            {
                var err = p.StandardError.ReadToEndAsync();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return (output + err.Result).Trim();
            }
        });
    }

    // 창 없이 실행하고 종료 코드만 받기
    public static int Run(string file, string args)
    {
        using (var p = Process.Start(Hidden(file, args)))
        {
            p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return p.ExitCode;
        }
    }

    // 프로그램/설정 창 열기
    public static void Open(string file, string args = "")
    {
        try { Process.Start(new ProcessStartInfo(file, args) { UseShellExecute = true }); }
        catch (Win32Exception) { } // UAC 취소 등
        catch (Exception ex) { MessageBox.Show("열 수 없습니다: " + ex.Message, "명령어 도구함"); }
    }

    // 관리자 권한 cmd 창에서 실행 (창이 남아 결과 확인 가능)
    public static bool RunAdmin(string command)
    {
        try
        {
            Process.Start(new ProcessStartInfo("cmd.exe", "/k " + command) { UseShellExecute = true, Verb = "runas" });
            return true;
        }
        catch (Win32Exception) { return false; }
    }

    // 관리자 권한으로 창 없이 실행, 종료 코드 반환 (UAC 취소 시 -1)
    public static Task<int> RunAdminHidden(string file, string args)
    {
        return Task.Run(() =>
        {
            try
            {
                using (var p = Process.Start(new ProcessStartInfo(file, args)
                    { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden }))
                {
                    p.WaitForExit();
                    return p.ExitCode;
                }
            }
            catch (Win32Exception) { return -1; }
        });
    }

    public static bool Confirm(string text)
    {
        return MessageBox.Show(text, "명령어 도구함", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    }
}

// ───────────────────────── 페이지 ─────────────────────────
class Page : Panel
{
    public Page(string title, string desc, string command)
    {
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Body;
        Dock = DockStyle.Fill;

        Controls.Add(new Label
        {
            Text = title, Font = new Font("Malgun Gothic", 17f, FontStyle.Bold),
            Location = new Point(32, 24), AutoSize = true
        });
        Controls.Add(new Label
        {
            Text = desc, ForeColor = Theme.Dim, Location = new Point(34, 66), Size = new Size(540, 22)
        });
        if (command != null)
            Controls.Add(new Label
            {
                Text = "> " + command, Font = Theme.Mono, ForeColor = Theme.Accent, BackColor = Theme.Surface,
                Location = new Point(34, 94), AutoSize = true, Padding = new Padding(6, 3, 6, 3)
            });
    }
}

// 명령을 실행하고 결과를 아래 칸에 보여주는 페이지
class CommandPage : Page
{
    public delegate Task<string> Action(CommandPage page);
    readonly TextBox output;
    readonly List<RoundButton> buttons = new List<RoundButton>();

    public CommandPage(string title, string desc, string command, params object[] nameAndActions)
        : base(title, desc, command)
    {
        int x = 32;
        for (int i = 0; i < nameAndActions.Length; i += 2)
        {
            var act = (Action)nameAndActions[i + 1];
            bool primary = i == 0;
            var b = new RoundButton
            {
                Text = (string)nameAndActions[i], Location = new Point(x, 136), Size = new Size(150, 42),
                Fill = primary ? Theme.Accent : Theme.Surface, HoverFill = primary ? Theme.AccentHover : Theme.SurfaceHover,
                ForeColor = primary ? Theme.Bg : Theme.Text,
                Font = primary ? new Font("Malgun Gothic", 10f, FontStyle.Bold) : Theme.Body
            };
            b.Click += async (s, e) => await Execute(act);
            buttons.Add(b);
            Controls.Add(b);
            x += 160;
        }

        var card = new Card { Location = new Point(32, 196), Size = new Size(536, 336) };
        output = new TextBox
        {
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, WordWrap = true,
            BorderStyle = BorderStyle.None, BackColor = Theme.Surface, ForeColor = Theme.Text, Font = Theme.Mono,
            Location = new Point(16, 14), Size = new Size(510, 310),
            Text = "버튼을 누르면 결과가 여기에 표시됩니다."
        };
        Theme.Dark(output);
        card.Controls.Add(output);
        Controls.Add(card);
    }

    public void Show(string text) { output.Text = text.Replace("\r\n", "\n").Replace("\n", "\r\n"); }

    async Task Execute(Action act)
    {
        foreach (var b in buttons) b.Enabled = false;
        string before = output.Text;
        Show("실행 중...");
        string result;
        try { result = await act(this); }
        catch (Exception ex) { result = "오류: " + ex.Message; }
        Show(result ?? before);
        foreach (var b in buttons) b.Enabled = true;
    }
}

// 누르면 바로 실행되는 타일 모음 페이지
class TilesPage : Page
{
    public TilesPage(string title, string desc, params object[] tiles) : base(title, desc, null)
    {
        for (int i = 0; i < tiles.Length / 3; i++)
        {
            var act = (System.Action)tiles[i * 3 + 2];
            var b = new RoundButton
            {
                Text = (string)tiles[i * 3], Sub = (string)tiles[i * 3 + 1], Radius = 12,
                Location = new Point(32 + (i % 3) * 182, 110 + (i / 3) * 76), Size = new Size(172, 66)
            };
            b.Click += (s, e) => act();
            Controls.Add(b);
        }
    }
}

// 1번: 종료 예약
class ShutdownPage : Page
{
    NumericUpDown hours, minutes;
    Ring ring;
    Timer tick;
    DateTime? target;
    int totalSec;

    public ShutdownPage() : base("종료 예약", "지정한 시간이 지나면 PC를 자동으로 종료합니다.", "shutdown -s -t <초>")
    {
        ring = new Ring { Location = new Point(32, 140), Size = new Size(220, 220) };
        Controls.Add(ring);

        hours = AddStepper(280, 150, "시간", 999, 0, 1);
        minutes = AddStepper(280, 216, "분", 59, 30, 5);
        hours.ValueChanged += (s, e) => UpdateStatus();
        minutes.ValueChanged += (s, e) => UpdateStatus();

        int x = 280;
        foreach (int m in new[] { 10, 30, 60, 120 })
        {
            int mm = m;
            var b = new RoundButton
            {
                Text = m < 60 ? m + "분" : (m / 60) + "시간",
                Location = new Point(x, 290), Size = new Size(66, 34), Radius = 17
            };
            b.Click += (s, e) => { hours.Value = mm / 60; minutes.Value = mm % 60; };
            Controls.Add(b);
            x += 74;
        }

        var start = new RoundButton
        {
            Text = "종료 예약", Location = new Point(32, 390), Size = new Size(536, 50),
            Fill = Theme.Accent, HoverFill = Theme.AccentHover, ForeColor = Theme.Bg, Radius = 12,
            Font = new Font("Malgun Gothic", 12f, FontStyle.Bold)
        };
        start.Click += (s, e) => Schedule();
        Controls.Add(start);

        var cancel = new RoundButton
        {
            Text = "예약 취소", Location = new Point(32, 450), Size = new Size(536, 42),
            Fill = Theme.Bg, HoverFill = Theme.Surface, Border = Theme.SurfaceHover, Radius = 12
        };
        cancel.Click += (s, e) => Cancel();
        Controls.Add(cancel);

        tick = new Timer { Interval = 1000 };
        tick.Tick += (s, e) => UpdateStatus();
        UpdateStatus();
    }

    // [−] 값 단위 [+] 형태의 입력 칸
    NumericUpDown AddStepper(int x, int y, string unit, int max, int value, int step)
    {
        var card = new Card { Location = new Point(x, y), Size = new Size(288, 56) };
        var nud = new NumericUpDown
        {
            Maximum = max, Value = value, BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Right,
            BackColor = Theme.Surface, ForeColor = Theme.Text, Font = new Font("Segoe UI Semibold", 15f),
            Location = new Point(70, 14), Width = 80
        };
        nud.Controls[0].Hide(); // 기본 화살표 숨김

        var minus = new RoundButton { Text = "−", Location = new Point(10, 12), Size = new Size(32, 32), Radius = 16, BackColor = Theme.Surface, Fill = Theme.SurfaceHover, HoverFill = Theme.Dim };
        var plus = new RoundButton { Text = "+", Location = new Point(246, 12), Size = new Size(32, 32), Radius = 16, BackColor = Theme.Surface, Fill = Theme.SurfaceHover, HoverFill = Theme.Dim };
        minus.Click += (s, e) => Step(nud, -step);
        plus.Click += (s, e) => Step(nud, step);

        var lbl = new Label { Text = unit, ForeColor = Theme.Dim, BackColor = Theme.Surface, AutoSize = true, Location = new Point(156, 19) };
        card.Controls.AddRange(new Control[] { minus, nud, lbl, plus });
        Controls.Add(card);
        return nud;
    }

    void Step(NumericUpDown nud, int delta)
    {
        decimal v = nud.Value + delta;
        if (nud == minutes && v > 59) { v = 0; if (hours.Value < hours.Maximum) hours.Value++; }
        else if (nud == minutes && v < 0) { if (hours.Value > 0) { hours.Value--; v = 60 + v; } else v = 0; }
        nud.Value = Math.Max(nud.Minimum, Math.Min(nud.Maximum, v));
    }

    void Schedule()
    {
        int sec = (int)(hours.Value * 3600 + minutes.Value * 60);
        if (sec <= 0) { MessageBox.Show("시간을 입력하세요.", "종료 예약"); return; }
        Shell.Run("shutdown", "-a"); // 기존 예약이 있으면 먼저 취소 (없으면 무시됨)
        int code = Shell.Run("shutdown", "-s -t " + sec);
        if (code != 0) { MessageBox.Show("예약 실패 (코드 " + code + ")", "종료 예약"); return; }
        totalSec = sec;
        target = DateTime.Now.AddSeconds(sec);
        tick.Start();
        UpdateStatus();
    }

    void Cancel()
    {
        int code = Shell.Run("shutdown", "-a");
        target = null;
        tick.Stop();
        UpdateStatus();
        ring.Small = code == 0 ? "예약이 취소되었습니다" : "취소할 예약이 없습니다";
        ring.Invalidate();
    }

    static string Fmt(TimeSpan t) { return string.Format("{0}:{1:mm\\:ss}", (int)t.TotalHours, t); }

    void UpdateStatus()
    {
        if (target == null)
        {
            ring.Active = false;
            ring.Big = Fmt(TimeSpan.FromSeconds((double)(hours.Value * 3600 + minutes.Value * 60)));
            ring.Small = "예약 없음";
        }
        else
        {
            TimeSpan left = target.Value - DateTime.Now;
            if (left < TimeSpan.Zero) left = TimeSpan.Zero;
            ring.Active = true;
            ring.Progress = (float)(left.TotalSeconds / totalSec);
            ring.Big = Fmt(left);
            ring.Small = target.Value.ToString("HH:mm") + " 종료 예정";
        }
        ring.Invalidate();
    }
}

// ───────────────────────── 기능 구현 ─────────────────────────
static class Features
{
    [DllImport("user32.dll")] static extern bool LockWorkStation();

    public static Page Power()
    {
        return new TilesPage("전원", "잠금, 절전, 로그아웃, 재시작을 바로 실행합니다.",
            "화면 잠금", "LockWorkStation", (Action)(() => LockWorkStation()),
            "절전 모드", "SetSuspendState", (Action)(() => Application.SetSuspendState(PowerState.Suspend, false, false)),
            "로그아웃", "shutdown -l", (Action)(() => { if (Shell.Confirm("로그아웃할까요?\n저장하지 않은 작업은 사라집니다.")) Shell.Run("shutdown", "-l"); }),
            "지금 재시작", "shutdown -r -t 0", (Action)(() => { if (Shell.Confirm("지금 재시작할까요?\n저장하지 않은 작업은 사라집니다.")) Shell.Run("shutdown", "-r -t 0"); }),
            "지금 종료", "shutdown -s -t 0", (Action)(() => { if (Shell.Confirm("지금 PC를 끌까요?\n저장하지 않은 작업은 사라집니다.")) Shell.Run("shutdown", "-s -t 0"); }),
            "예약 취소", "shutdown -a", (Action)(() =>
                MessageBox.Show(Shell.Run("shutdown", "-a") == 0 ? "종료/재시작 예약을 취소했습니다." : "취소할 예약이 없습니다.", "명령어 도구함")));
    }

    public static Page Ip()
    {
        return new CommandPage("IP 확인", "이 PC의 네트워크 주소(IPv4, 게이트웨이 등)를 확인합니다.", "ipconfig",
            "IP 확인", (CommandPage.Action)(p => Shell.Capture("ipconfig", "")),
            "자세히 (/all)", (CommandPage.Action)(p => Shell.Capture("ipconfig", "/all")));
    }

    public static Page Ping()
    {
        return new CommandPage("인터넷 연결 확인", "외부 서버에 신호를 보내 인터넷이 되는지, 응답 속도는 어떤지 확인합니다.", "ping google.com",
            "연결 확인", (CommandPage.Action)(async p =>
            {
                string a = await Shell.Capture("ping", "-n 4 8.8.8.8");
                string b = await Shell.Capture("ping", "-n 4 google.com");
                return "[ 8.8.8.8 (인터넷 연결) ]\n" + a + "\n\n[ google.com (DNS 포함) ]\n" + b;
            }),
            "경로 추적", (CommandPage.Action)(p => Shell.Capture("tracert", "-d -h 15 8.8.8.8")));
    }

    public static Page Dns()
    {
        return new CommandPage("DNS 캐시 비우기", "사이트가 갑자기 안 열리거나 주소가 바뀐 사이트에 접속이 안 될 때 사용합니다.", "ipconfig /flushdns",
            "DNS 비우기", (CommandPage.Action)(async p =>
            {
                int code = await Shell.RunAdminHidden("ipconfig", "/flushdns");
                return code == 0 ? "DNS 확인자 캐시를 비웠습니다."
                     : code == -1 ? "관리자 권한 요청이 취소되었습니다."
                     : "실패했습니다. (코드 " + code + ")";
            }),
            "캐시 보기", (CommandPage.Action)(p => Shell.Capture("ipconfig", "/displaydns")));
    }

    public static Page Wifi()
    {
        return new CommandPage("와이파이 비밀번호", "이 PC에 저장된 와이파이 이름과 비밀번호를 보여줍니다.", "netsh wlan show profile key=clear",
            "비밀번호 보기", (CommandPage.Action)(async p =>
            {
                string list = await Shell.Capture("netsh", "wlan show profiles");
                var names = new List<string>();
                foreach (string line in list.Split('\n'))
                    if ((line.Contains("All User Profile") || line.Contains("모든 사용자 프로필")) && line.Contains(":"))
                        names.Add(line.Substring(line.IndexOf(':') + 1).Trim());
                if (names.Count == 0) return "저장된 와이파이를 찾지 못했습니다.\n\n" + list;

                var sb = new StringBuilder();
                foreach (string name in names)
                {
                    string info = await Shell.Capture("netsh", "wlan show profile name=\"" + name + "\" key=clear");
                    string key = "(비밀번호 없음 또는 확인 불가)";
                    foreach (string line in info.Split('\n'))
                        if ((line.Contains("Key Content") || line.Contains("키 콘텐츠")) && line.Contains(":"))
                            key = line.Substring(line.IndexOf(':') + 1).Trim();
                    sb.AppendLine(name);
                    sb.AppendLine("    " + key);
                    sb.AppendLine();
                }
                return sb.ToString();
            }));
    }

    public static Page Temp()
    {
        string temp = Path.GetTempPath();
        return new CommandPage("임시 파일 정리", "임시 폴더(%TEMP%)의 파일을 지웁니다. 사용 중인 파일은 건너뜁니다.", "del /s /q %TEMP%\\*",
            "정리하기", (CommandPage.Action)(async p =>
            {
                if (!Shell.Confirm("임시 폴더의 파일을 삭제할까요?\n" + temp)) return "취소했습니다.";
                return await Task.Run(() =>
                {
                    long bytes = 0; int deleted = 0, skipped = 0;
                    Clean(temp, false, ref bytes, ref deleted, ref skipped);
                    return string.Format("정리 완료\n\n삭제한 파일: {0}개\n확보한 용량: {1:0.0} MB\n사용 중이라 건너뜀: {2}개",
                        deleted, bytes / 1048576.0, skipped);
                });
            }),
            "폴더 열기", (CommandPage.Action)(p => { Shell.Open(temp); return Task.FromResult<string>(null); }));
    }

    static void Clean(string dir, bool removeSelf, ref long bytes, ref int deleted, ref int skipped)
    {
        try
        {
            foreach (string f in Directory.GetFiles(dir))
            {
                try { long len = new FileInfo(f).Length; File.Delete(f); bytes += len; deleted++; }
                catch { skipped++; }
            }
            foreach (string d in Directory.GetDirectories(dir))
                Clean(d, true, ref bytes, ref deleted, ref skipped);
            if (removeSelf && Directory.GetFileSystemEntries(dir).Length == 0) Directory.Delete(dir);
        }
        catch { }
    }

    public static Page SystemInfo()
    {
        return new CommandPage("시스템 정보", "Windows 버전, CPU, 메모리, 설치 날짜 등을 확인합니다. (몇 초 걸립니다)", "systeminfo",
            "정보 보기", (CommandPage.Action)(p => Shell.Capture("systeminfo", "")),
            "디스크 용량", (CommandPage.Action)(p => Task.FromResult(DiskInfo())));
    }

    static string DiskInfo()
    {
        var sb = new StringBuilder();
        foreach (var d in DriveInfo.GetDrives())
        {
            if (!d.IsReady) continue;
            double total = d.TotalSize / 1073741824.0, free = d.AvailableFreeSpace / 1073741824.0;
            sb.AppendLine(string.Format("{0}  {1,-10} 전체 {2,7:0.0} GB   남은 {3,7:0.0} GB  ({4:0}% 사용)",
                d.Name, d.VolumeLabel, total, free, (total - free) / total * 100));
        }
        return sb.ToString();
    }

    public static Page Battery()
    {
        string path = Path.Combine(Path.GetTempPath(), "battery-report.html");
        return new CommandPage("배터리 리포트", "노트북 배터리의 설계 용량 대비 현재 용량, 사용 기록을 보고서로 만들어 엽니다.", "powercfg /batteryreport",
            "리포트 만들기", (CommandPage.Action)(async p =>
            {
                string r = await Shell.Capture("powercfg", "/batteryreport /output \"" + path + "\"");
                if (File.Exists(path)) Shell.Open(path);
                return r;
            }));
    }

    public static Page Sfc()
    {
        return new CommandPage("시스템 파일 검사", "손상된 Windows 시스템 파일을 찾아 복구합니다. 관리자 권한 창이 열리고 10분 이상 걸릴 수 있습니다.", "sfc /scannow",
            "SFC 검사", (CommandPage.Action)(p => Task.FromResult(Shell.RunAdmin("sfc /scannow")
                ? "관리자 권한 cmd 창에서 검사를 시작했습니다.\n진행 상황은 새로 열린 창에서 확인하세요."
                : "관리자 권한 요청이 취소되었습니다.")),
            "DISM 복구", (CommandPage.Action)(p => Task.FromResult(Shell.RunAdmin("DISM /Online /Cleanup-Image /RestoreHealth")
                ? "관리자 권한 cmd 창에서 DISM 복구를 시작했습니다.\nSFC로 해결되지 않을 때 사용하고, 끝나면 SFC를 한 번 더 돌리세요."
                : "관리자 권한 요청이 취소되었습니다.")));
    }

    public static Page Tools()
    {
        return new TilesPage("Windows 도구", "cmd나 실행 창(Win+R)에 입력하던 도구를 바로 엽니다.",
            "작업 관리자", "taskmgr", (Action)(() => Shell.Open("taskmgr")),
            "제어판", "control", (Action)(() => Shell.Open("control")),
            "장치 관리자", "devmgmt.msc", (Action)(() => Shell.Open("devmgmt.msc")),
            "서비스", "services.msc", (Action)(() => Shell.Open("services.msc")),
            "디스크 관리", "diskmgmt.msc", (Action)(() => Shell.Open("diskmgmt.msc")),
            "시스템 구성", "msconfig", (Action)(() => Shell.Open("msconfig")),
            "레지스트리 편집기", "regedit", (Action)(() => Shell.Open("regedit")),
            "환경 변수", "sysdm.cpl", (Action)(() => Shell.Open("rundll32.exe", "sysdm.cpl,EditEnvironmentVariables")),
            "네트워크 연결", "ncpa.cpl", (Action)(() => Shell.Open("ncpa.cpl")),
            "프로그램 제거", "appwiz.cpl", (Action)(() => Shell.Open("appwiz.cpl")),
            "명령 프롬프트", "cmd", (Action)(() => Shell.Open("cmd.exe")),
            "임시 폴더", "%temp%", (Action)(() => Shell.Open(Path.GetTempPath())));
    }
}

// ───────────────────────── 메인 창 ─────────────────────────
class MainForm : Form
{
    readonly Panel content;
    readonly List<NavItem> navs = new List<NavItem>();
    readonly Dictionary<NavItem, Page> pages = new Dictionary<NavItem, Page>();

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    MainForm()
    {
        Text = "명령어 도구함";
        Font = Theme.Body;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(820, 560);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        content = new Panel { Location = new Point(220, 0), Size = new Size(600, 560), BackColor = Theme.Bg };
        Controls.Add(content);

        var side = new Panel { Location = new Point(0, 0), Size = new Size(220, 560), BackColor = Theme.Sidebar };
        side.Controls.Add(new Label { Text = "명령어 도구함", Font = new Font("Malgun Gothic", 13f, FontStyle.Bold), Location = new Point(20, 22), AutoSize = true });
        side.Controls.Add(new Label { Text = "cmd 명령어를 버튼 하나로", ForeColor = Theme.Dim, Location = new Point(21, 50), AutoSize = true });
        Controls.Add(side);

        var list = new List<KeyValuePair<string, Func<Page>>>
        {
            Item("종료 예약", () => new ShutdownPage()),
            Item("전원", Features.Power),
            Item("IP 확인", Features.Ip),
            Item("인터넷 연결 확인", Features.Ping),
            Item("DNS 캐시 비우기", Features.Dns),
            Item("와이파이 비밀번호", Features.Wifi),
            Item("임시 파일 정리", Features.Temp),
            Item("시스템 정보", Features.SystemInfo),
            Item("배터리 리포트", Features.Battery),
            Item("시스템 파일 검사", Features.Sfc),
            Item("Windows 도구", Features.Tools),
        };

        for (int i = 0; i < list.Count; i++)
        {
            var nav = new NavItem { Number = i + 1, Text = list[i].Key, Location = new Point(10, 84 + i * 40), Size = new Size(200, 38) };
            var make = list[i].Value;
            nav.Click += (s, e) => Select(nav, make);
            navs.Add(nav);
            side.Controls.Add(nav);
        }
        Select(navs[0], list[0].Value);
    }

    static KeyValuePair<string, Func<Page>> Item(string name, Func<Page> make)
    {
        return new KeyValuePair<string, Func<Page>>(name, make);
    }

    // 페이지는 처음 열 때 만들고, 이후엔 재사용 (종료 예약 카운트다운 유지)
    void Select(NavItem nav, Func<Page> make)
    {
        foreach (var n in navs) { n.Selected = n == nav; n.Invalidate(); }
        Page page;
        if (!pages.TryGetValue(nav, out page))
        {
            page = make();
            pages[nav] = page;
            content.Controls.Add(page);
        }
        foreach (var p in pages.Values) p.Visible = p == page;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int on = 1;
        try { DwmSetWindowAttribute(Handle, 20, ref on, 4); } catch { } // 다크 제목 표시줄
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
