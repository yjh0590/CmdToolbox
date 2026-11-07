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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

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

    // 화면 배율 (100% = 1, 150% = 1.5). 레이아웃은 100% 기준 픽셀로 쓰고 이 값을 곱한다
    public static float Factor = 1f;
    public static int S(float v) { return (int)Math.Round(v * Factor); }

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

    protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
}

// 둥근 버튼 (Sub가 있으면 두 줄 타일)
class RoundButton : PaintedControl
{
    public Color Fill = Theme.Surface, HoverFill = Theme.SurfaceHover, Border = Color.Empty;
    public float Radius = 10;
    public string Sub = "";
    bool hover, down;

    public RoundButton() { Cursor = Cursors.Hand; }

    public void PerformClick() { if (Enabled) OnClick(EventArgs.Empty); }

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
        using (var path = Theme.Round(r, Radius * Theme.Factor))
        {
            using (var b = new SolidBrush(fill)) g.FillPath(b, path);
            if (Border != Color.Empty)
                using (var pen = new Pen(hover && Enabled ? Theme.Dim : Border, 1.5f * Theme.Factor)) g.DrawPath(pen, path);
        }
        if (Sub == "")
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, fore, Theme.Center);
        else
        {
            TextRenderer.DrawText(g, Text, Font, new Rectangle(Theme.S(6), Height / 2 - Theme.S(21), Width - Theme.S(12), Theme.S(22)), fore, Theme.Center);
            TextRenderer.DrawText(g, Sub, Theme.MonoSmall, new Rectangle(Theme.S(6), Height / 2 + Theme.S(2), Width - Theme.S(12), Theme.S(18)), Theme.Dim, Theme.Center);
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
            using (var path = Theme.Round(new RectangleF(0, 0, Width - 1, Height - 1), 8 * Theme.Factor))
            using (var b = new SolidBrush(Selected ? Theme.Surface : Color.FromArgb(26, 26, 36)))
                g.FillPath(b, path);
        if (Selected)
            using (var path = Theme.Round(new RectangleF(0, Theme.S(9), Theme.S(4), Height - Theme.S(18)), 2 * Theme.Factor))
            using (var b = new SolidBrush(Theme.Accent))
                g.FillPath(b, path);

        TextRenderer.DrawText(g, Number.ToString(), Theme.Mono, new Rectangle(Theme.S(12), 0, Theme.S(24), Height),
            Selected ? Theme.Accent : Theme.Dim, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(Theme.S(46), 0, Width - Theme.S(50), Height),
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
        using (var path = Theme.Round(new RectangleF(0, 0, Width - 1, Height - 1), 12 * Theme.Factor))
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
        float m = 10 * Theme.Factor, w = 12 * Theme.Factor;
        var r = new RectangleF(m, m, Width - 2 * m, Height - 2 * m);
        using (var track = new Pen(Theme.Surface, w)) g.DrawEllipse(track, r);
        if (Active && Progress > 0)
            using (var arc = new Pen(Theme.Accent, w) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(arc, r, -90, 360 * Progress);

        TextRenderer.DrawText(g, Big, bigFont, new Rectangle(0, Height / 2 - Theme.S(38), Width, Theme.S(54)),
            Active ? Theme.Text : Theme.Dim, Theme.Center);
        TextRenderer.DrawText(g, Small, Font, new Rectangle(0, Height / 2 + Theme.S(18), Width, Theme.S(24)),
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

    // 창 없이 실행하면서 출력을 한 줄씩 넘겨주고, 끝나면 전체 출력을 돌려줌.
    // token이 취소되면 프로세스를 끝내고 OperationCanceledException을 던짐
    public static Task<string> Stream(string file, string args, Action<string> onLine, CancellationToken token)
    {
        return Task.Run(() =>
        {
            var all = new StringBuilder();
            using (var p = Process.Start(Hidden(file, args)))
            {
                Action<string> add = line =>
                {
                    lock (all) all.AppendLine(line);
                    if (onLine != null) onLine(line);
                };
                var o = Task.Run(() => Pump(p.StandardOutput.BaseStream, add));
                var e = Task.Run(() => Pump(p.StandardError.BaseStream, add));
                using (token.Register(() => { try { p.Kill(); } catch { } }))
                {
                    p.WaitForExit();
                    Task.WaitAll(o, e);
                }
                token.ThrowIfCancellationRequested();
                lock (all) return all.ToString().Trim();
            }
        });
    }

    // 출력을 줄 단위로 읽음. 명령마다 인코딩이 달라서(netsh는 UTF-8, ipconfig는 CP949) 줄마다 판별
    static void Pump(Stream s, Action<string> onLine)
    {
        var line = new MemoryStream();
        var input = new BufferedStream(s);
        int b;
        while ((b = input.ReadByte()) != -1)
        {
            if (b == '\n') { onLine(Decode(line)); line.SetLength(0); }
            else if (b != '\r') line.WriteByte((byte)b);
        }
        if (line.Length > 0) onLine(Decode(line));
    }

    static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    static string Decode(MemoryStream line)
    {
        byte[] bytes = line.ToArray();
        try { return StrictUtf8.GetString(bytes); }
        catch (DecoderFallbackException) { return Oem.GetString(bytes); }
    }

    public static Task<string> Capture(string file, string args, CancellationToken token)
    {
        return Stream(file, args, null, token);
    }

    // 창 없이 실행하고 종료 코드만 받기
    public static int Run(string file, string args)
    {
        using (var p = Process.Start(Hidden(file, args)))
        {
            var err = p.StandardError.ReadToEndAsync();
            p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            err.Wait();
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

// ───────────────────────── 종료 예약 상태 ─────────────────────────
// 예약은 여기서만 걸고 취소한다. 앱을 다시 열어도 남아 있도록 파일에 저장
static class ShutdownPlan
{
    public static DateTime? Target { get; private set; }
    public static int TotalSec { get; private set; }
    public static event System.Action Changed;

    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CmdToolbox", "shutdown.txt");

    [DllImport("kernel32.dll")] static extern ulong GetTickCount64();

    // 저장된 예약 불러오기. 시각이 지났거나, 저장한 뒤 PC가 다시 켜졌으면 (예약이 사라졌으므로) 버림
    public static void Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            string[] parts = File.ReadAllText(FilePath).Split(' ');
            var target = new DateTime(long.Parse(parts[0]));
            int total = int.Parse(parts[1]);
            var saved = new DateTime(long.Parse(parts[2]));
            var boot = DateTime.Now.AddMilliseconds(-(double)GetTickCount64());
            if (target > DateTime.Now && saved > boot) { Target = target; TotalSec = total; }
            else Forget();
        }
        catch { Forget(); }
    }

    // 성공하면 0, 실패하면 shutdown 종료 코드
    public static int Start(int sec)
    {
        Shell.Run("shutdown", "-a"); // 기존 예약이 있으면 먼저 취소 (없으면 무시됨)
        int code = Shell.Run("shutdown", "-s -t " + sec);
        if (code != 0) { Forget(); Notify(); return code; }
        Target = DateTime.Now.AddSeconds(sec);
        TotalSec = sec;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, Target.Value.Ticks + " " + sec + " " + DateTime.Now.Ticks);
        }
        catch { }
        Notify();
        return 0;
    }

    // 예약이 있어서 취소했으면 true
    public static bool Cancel()
    {
        bool ok = Shell.Run("shutdown", "-a") == 0;
        Forget();
        Notify();
        return ok;
    }

    public static void Forget()
    {
        Target = null;
        try { File.Delete(FilePath); } catch { }
    }

    static void Notify() { var h = Changed; if (h != null) h(); }
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
            Text = desc, ForeColor = Theme.Dim, Location = new Point(34, 66), Size = new Size(540, 22), AutoEllipsis = true
        });
        if (command != null)
            Controls.Add(new Label
            {
                Text = "> " + command, Font = Theme.Mono, ForeColor = Theme.Accent, BackColor = Theme.Surface,
                Location = new Point(34, 94), AutoSize = true, Padding = new Padding(6, 3, 6, 3)
            });
    }
}

// 명령을 실행하고 결과를 아래 칸에 보여주는 페이지.
// 실행 중에는 누른 버튼이 "중지"로 바뀌고, 출력은 나오는 대로 바로 표시된다
class CommandPage : Page
{
    // 결과 문자열을 돌려주면 그 내용으로 표시, null이면 실행 중 출력한 내용을 그대로 둠
    public delegate Task<string> Action(CommandPage page);

    readonly TextBox output;
    readonly List<RoundButton> buttons = new List<RoundButton>();
    readonly List<Control> slots = new List<Control>(); // 위쪽 한 줄에 놓이는 입력 칸과 버튼들
    readonly StringBuilder pending = new StringBuilder();
    readonly Timer flush = new Timer { Interval = 100 };
    RoundButton running;
    CancellationTokenSource cts;
    bool fresh;   // 아직 "실행 중..."만 보이는 상태
    bool touched; // 이번 실행에서 출력을 쓴 적 있음
    string lastResult = "";

    public CommandPage(string title, string desc, string command, params object[] nameAndActions)
        : base(title, desc, command)
    {
        for (int i = 0; i < nameAndActions.Length; i += 2)
            AddButton((string)nameAndActions[i], (Action)nameAndActions[i + 1]);

        var card = new Card { Location = new Point(32, 196), Size = new Size(536, 336) };
        output = new TextBox
        {
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, // 표가 깨지지 않게 줄바꿈 안 함
            BorderStyle = BorderStyle.None, BackColor = Theme.Surface, ForeColor = Theme.Text, Font = Theme.Mono,
            Location = new Point(16, 14), Size = new Size(510, 310),
            Text = "버튼을 누르면 결과가 여기에 표시됩니다."
        };
        Theme.Dark(output);
        card.Controls.Add(output);
        Controls.Add(card);

        flush.Tick += (s, e) => Flush();
    }

    RoundButton NewButton(string text)
    {
        bool primary = buttons.Count == 0;
        var b = new RoundButton
        {
            Text = text, Size = new Size(150, 42),
            Fill = primary ? Theme.Accent : Theme.Surface, HoverFill = primary ? Theme.AccentHover : Theme.SurfaceHover,
            ForeColor = primary ? Theme.Bg : Theme.Text,
            Font = primary ? new Font("Malgun Gothic", 10f, FontStyle.Bold) : Theme.Body
        };
        buttons.Add(b);
        slots.Add(b);
        Controls.Add(b);
        Relayout();
        return b;
    }

    // 개수가 많으면 너비를 줄여서 한 줄에 맞춤
    void Relayout()
    {
        int n = slots.Count;
        int w = Math.Min(150, (536 - (n - 1) * 10) / n);
        for (int i = 0; i < n; i++)
            slots[i].Bounds = new Rectangle(32 + i * (w + 10), 136, w, 42);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wp, string lp);

    // 버튼 줄 맨 앞에 입력 칸 추가. Enter를 누르면 첫 번째 버튼 실행
    public TextBox AddInput(string hint)
    {
        var card = new Card();
        var box = new TextBox
        {
            BorderStyle = BorderStyle.None, BackColor = Theme.Surface, ForeColor = Theme.Text, Font = Theme.Body,
            Location = new Point(12, 11), Width = 126, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        box.HandleCreated += (s, e) => SendMessage(box.Handle, 0x1501, (IntPtr)1, hint); // 회색 안내 문구
        box.KeyDown += (s, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            if (running == null) buttons[0].PerformClick();
        };
        card.Size = new Size(150, 42);
        card.Controls.Add(box);
        slots.Insert(0, card);
        Controls.Add(card);
        Relayout();
        return box;
    }

    void AddButton(string text, Action act)
    {
        var b = NewButton(text);
        b.Click += async (s, e) =>
        {
            if (running == b) { cts.Cancel(); return; }
            if (running == null) await Execute(b, act);
        };
    }

    // 마지막 결과를 클립보드에 복사하는 버튼
    public void AddCopyButton()
    {
        var b = NewButton("결과 복사");
        var reset = new Timer { Interval = 1500 };
        reset.Tick += (s, e) => { reset.Stop(); b.Text = "결과 복사"; };
        b.Click += (s, e) =>
        {
            if (lastResult == "") return;
            try { Clipboard.SetText(lastResult); } catch (ExternalException) { return; }
            b.Text = "복사했습니다";
            reset.Stop();
            reset.Start();
        };
    }

    public CancellationToken Token { get { return cts.Token; } }

    // 출력을 실시간으로 보여주며 실행
    public Task<string> Run(string file, string args)
    {
        touched = true;
        return Shell.Stream(file, args, Enqueue, cts.Token);
    }

    // 결과 칸에 한 줄 추가
    public void Append(string line)
    {
        touched = true;
        Enqueue(line);
    }

    void Enqueue(string line) { lock (pending) pending.AppendLine(line); }

    static string Crlf(string text) { return text.Replace("\r\n", "\n").Replace("\n", "\r\n"); }

    void Flush()
    {
        string text;
        lock (pending)
        {
            if (pending.Length == 0) return;
            text = pending.ToString();
            pending.Clear();
        }
        if (fresh) { output.Clear(); fresh = false; }
        output.AppendText(Crlf(text));
    }

    void Show(string text) { output.Text = Crlf(text); }

    async Task Execute(RoundButton b, Action act)
    {
        string label = b.Text, before = output.Text;
        running = b;
        cts = new CancellationTokenSource();
        foreach (var x in buttons) x.Enabled = x == b;
        b.Text = "중지";
        Show("실행 중...");
        fresh = true;
        touched = false;
        flush.Start();

        string result = null;
        bool stopped = false;
        try { result = await act(this); }
        catch (OperationCanceledException) { stopped = true; }
        catch (Exception ex) { result = "오류: " + ex.Message; }

        flush.Stop();
        Flush();
        if (stopped) { if (fresh) Show("중지했습니다."); else output.AppendText("\r\n[중지했습니다]"); }
        else if (result != null) Show(result);
        else if (!touched) Show(before);
        else if (fresh) Show("(출력 없음)");
        fresh = false;
        lastResult = output.Text;

        running = null;
        cts.Dispose();
        cts = null;
        b.Text = label;
        foreach (var x in buttons) x.Enabled = true;
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

// 1번: 종료 예약 ("몇 시간 뒤" 또는 "몇 시 몇 분에")
class ShutdownPage : Page
{
    static readonly int[] AfterPresets = { 10, 30, 60, 120 }; // 분 뒤
    static readonly int[] AtPresets = { 22, 23, 0, 1 };        // 시 정각

    readonly NumericUpDown hours, minutes;
    readonly Label hoursUnit;
    readonly Ring ring;
    readonly RoundButton afterTab, atTab;
    readonly List<RoundButton> presets = new List<RoundButton>();
    bool clock; // true: 시각 지정, false: 시간 뒤
    string notice;
    DateTime noticeUntil;

    public ShutdownPage() : base("종료 예약", "정한 시간 뒤나 정한 시각에 PC를 자동으로 종료합니다.", "shutdown -s -t <초>")
    {
        ring = new Ring { Location = new Point(32, 140), Size = new Size(220, 220) };
        Controls.Add(ring);

        afterTab = AddTab("시간 뒤", 280);
        atTab = AddTab("시각 지정", 428);
        afterTab.Click += (s, e) => SetMode(false);
        atTab.Click += (s, e) => SetMode(true);

        Label minutesUnit;
        hours = AddStepper(184, 999, 1, out hoursUnit);
        minutes = AddStepper(250, 59, 5, out minutesUnit);
        minutesUnit.Text = "분";
        hours.ValueChanged += (s, e) => UpdateStatus();
        minutes.ValueChanged += (s, e) => UpdateStatus();

        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            var b = new RoundButton { Location = new Point(280 + i * 74, 318), Size = new Size(66, 34), Radius = 17 };
            b.Click += (s, e) => ApplyPreset(idx);
            presets.Add(b);
            Controls.Add(b);
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
        cancel.Click += (s, e) => Notice(ShutdownPlan.Cancel() ? "예약이 취소되었습니다" : "취소할 예약이 없습니다");
        Controls.Add(cancel);

        // 다른 페이지(전원 > 예약 취소)에서 바뀌어도 바로 반영
        System.Action changed = () => { if (IsHandleCreated) BeginInvoke((MethodInvoker)UpdateStatus); };
        ShutdownPlan.Changed += changed;
        Disposed += (s, e) => ShutdownPlan.Changed -= changed;

        var tick = new Timer { Interval = 1000 };
        tick.Tick += (s, e) => UpdateStatus();
        tick.Start();
        SetMode(false);
    }

    RoundButton AddTab(string text, int x)
    {
        var b = new RoundButton { Text = text, Location = new Point(x, 140), Size = new Size(140, 34), Radius = 17 };
        Controls.Add(b);
        return b;
    }

    // [−] 값 단위 [+] 형태의 입력 칸
    NumericUpDown AddStepper(int y, int max, int step, out Label unit)
    {
        var card = new Card { Location = new Point(280, y), Size = new Size(288, 56) };
        var nud = new NumericUpDown
        {
            Maximum = max, BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Right,
            BackColor = Theme.Surface, ForeColor = Theme.Text, Font = new Font("Segoe UI Semibold", 15f)
        };
        // 기본 화살표를 숨기고, 화살표가 있던 자리(흰 칸)는 바깥 패널로 잘라서 안 보이게 함
        var arrows = nud.Controls[0];
        arrows.Hide();
        nud.Width = 80 + arrows.Width;
        var clip = new Panel { Location = new Point(70, 14), Size = new Size(80, nud.Height), BackColor = Theme.Surface };
        clip.Controls.Add(nud);

        var minus = new RoundButton { Text = "−", Location = new Point(10, 12), Size = new Size(32, 32), Radius = 16, BackColor = Theme.Surface, Fill = Theme.SurfaceHover, HoverFill = Theme.Dim };
        var plus = new RoundButton { Text = "+", Location = new Point(246, 12), Size = new Size(32, 32), Radius = 16, BackColor = Theme.Surface, Fill = Theme.SurfaceHover, HoverFill = Theme.Dim };
        minus.Click += (s, e) => Step(nud, -step);
        plus.Click += (s, e) => Step(nud, step);

        unit = new Label { ForeColor = Theme.Dim, BackColor = Theme.Surface, AutoSize = true, Location = new Point(156, 19) };
        card.Controls.AddRange(new Control[] { minus, clip, unit, plus });
        Controls.Add(card);
        return nud;
    }

    void SetMode(bool at)
    {
        clock = at;
        foreach (var t in new[] { afterTab, atTab })
        {
            bool sel = (t == atTab) == at;
            t.Fill = sel ? Theme.SurfaceHover : Theme.Bg;
            t.HoverFill = sel ? Theme.SurfaceHover : Theme.Surface;
            t.Border = sel ? Color.Empty : Theme.SurfaceHover;
            t.ForeColor = sel ? Theme.Text : Theme.Dim;
            t.Invalidate();
        }
        hoursUnit.Text = at ? "시" : "시간";
        for (int i = 0; i < presets.Count; i++)
        {
            int m = AfterPresets[i];
            presets[i].Text = at ? AtPresets[i] + "시" : m < 60 ? m + "분" : (m / 60) + "시간";
        }
        if (at)
        {
            hours.Maximum = 23;
            hours.Value = DateTime.Now.AddHours(1).Hour; // 기본값: 다음 정각
            minutes.Value = 0;
        }
        else
        {
            hours.Maximum = 999;
            hours.Value = 0;
            minutes.Value = 30;
        }
        UpdateStatus();
    }

    void ApplyPreset(int i)
    {
        if (clock) { hours.Value = AtPresets[i]; minutes.Value = 0; }
        else { hours.Value = AfterPresets[i] / 60; minutes.Value = AfterPresets[i] % 60; }
    }

    void Step(NumericUpDown nud, int delta)
    {
        if (clock) // 시각은 23:59 다음이 00:00
        {
            int total = (int)(hours.Value * 60 + minutes.Value) + (nud == hours ? delta * 60 : delta);
            total = (total % 1440 + 1440) % 1440;
            hours.Value = total / 60;
            minutes.Value = total % 60;
            return;
        }
        decimal v = nud.Value + delta;
        if (nud == minutes && v > 59) { v = 0; if (hours.Value < hours.Maximum) hours.Value++; }
        else if (nud == minutes && v < 0) { if (hours.Value > 0) { hours.Value--; v = 60 + v; } else v = 0; }
        nud.Value = Math.Max(nud.Minimum, Math.Min(nud.Maximum, v));
    }

    // 지금 입력한 값으로 예약하면 꺼질 시각
    DateTime Planned()
    {
        var now = DateTime.Now;
        if (!clock) return now.AddSeconds((double)(hours.Value * 3600 + minutes.Value * 60));
        var t = now.Date.AddHours((double)hours.Value).AddMinutes((double)minutes.Value);
        return t <= now ? t.AddDays(1) : t;
    }

    void Schedule()
    {
        int sec = (int)Math.Ceiling((Planned() - DateTime.Now).TotalSeconds);
        if (sec <= 0) { MessageBox.Show("시간을 입력하세요.", "종료 예약"); return; }
        int code = ShutdownPlan.Start(sec);
        if (code != 0) MessageBox.Show("예약 실패 (코드 " + code + ")", "종료 예약");
    }

    void Notice(string text)
    {
        notice = text;
        noticeUntil = DateTime.Now.AddSeconds(3);
        UpdateStatus();
    }

    static string Fmt(TimeSpan t) { return string.Format("{0}:{1:mm\\:ss}", (int)t.TotalHours, t); }

    static string When(DateTime t)
    {
        int days = (t.Date - DateTime.Today).Days;
        string hm = t.ToString("HH:mm");
        return days == 0 ? hm : days == 1 ? "내일 " + hm : t.ToString("M/d ") + hm;
    }

    void UpdateStatus()
    {
        // 예약 시각이 한참 지났는데 아직 켜져 있으면 (밖에서 취소했거나 해서) 예약 없음으로 처리
        if (ShutdownPlan.Target != null && ShutdownPlan.Target.Value < DateTime.Now.AddSeconds(-30))
            ShutdownPlan.Forget();

        if (ShutdownPlan.Target == null)
        {
            ring.Active = false;
            if (clock)
            {
                var t = Planned();
                ring.Big = Fmt(TimeSpan.FromSeconds(Math.Ceiling((t - DateTime.Now).TotalSeconds)));
                ring.Small = "예약 없음 · " + When(t);
            }
            else
            {
                ring.Big = Fmt(TimeSpan.FromSeconds((double)(hours.Value * 3600 + minutes.Value * 60)));
                ring.Small = "예약 없음";
            }
        }
        else
        {
            DateTime target = ShutdownPlan.Target.Value;
            TimeSpan left = target - DateTime.Now;
            if (left < TimeSpan.Zero) left = TimeSpan.Zero;
            ring.Active = true;
            ring.Progress = (float)(left.TotalSeconds / ShutdownPlan.TotalSec);
            ring.Big = Fmt(left);
            ring.Small = When(target) + " 종료 예정";
        }
        if (DateTime.Now < noticeUntil) ring.Small = notice;
        ring.Invalidate();
    }
}

// ───────────────────────── 기능 구현 ─────────────────────────
static class Features
{
    [DllImport("user32.dll")] static extern bool LockWorkStation();

    // 명령 출력을 그대로 실시간 표시
    static CommandPage.Action Live(string file, string args)
    {
        return async p => { await p.Run(file, args); return null; };
    }

    // 예약이 걸려 있으면 shutdown -r/-s 가 실패하므로 먼저 취소
    static void PowerNow(string question, string args)
    {
        if (!Shell.Confirm(question + "\n저장하지 않은 작업은 사라집니다.")) return;
        if (ShutdownPlan.Target != null) ShutdownPlan.Cancel();
        else Shell.Run("shutdown", "-a");
        Shell.Run("shutdown", args);
    }

    public static Page Power()
    {
        return new TilesPage("전원", "잠금, 절전, 로그아웃, 재시작을 바로 실행합니다.",
            "화면 잠금", "LockWorkStation", (Action)(() => LockWorkStation()),
            "절전 모드", "SetSuspendState", (Action)(() => Application.SetSuspendState(PowerState.Suspend, false, false)),
            "로그아웃", "shutdown -l", (Action)(() => { if (Shell.Confirm("로그아웃할까요?\n저장하지 않은 작업은 사라집니다.")) Shell.Run("shutdown", "-l"); }),
            "지금 재시작", "shutdown -r -t 0", (Action)(() => PowerNow("지금 재시작할까요?", "-r -t 0")),
            "지금 종료", "shutdown -s -t 0", (Action)(() => PowerNow("지금 PC를 끌까요?", "-s -t 0")),
            "예약 취소", "shutdown -a", (Action)(() =>
                MessageBox.Show(ShutdownPlan.Cancel() ? "종료/재시작 예약을 취소했습니다." : "취소할 예약이 없습니다.", "명령어 도구함")));
    }

    public static Page Ip()
    {
        return new CommandPage("IP 확인", "이 PC의 네트워크 주소(IPv4, 게이트웨이 등)를 확인합니다.", "ipconfig",
            "IP 확인", Live("ipconfig", ""),
            "자세히 (/all)", Live("ipconfig", "/all"),
            "공인 IP", (CommandPage.Action)(async p =>
            {
                string ip = await Shell.Capture("curl", "-s -m 8 https://ifconfig.me", p.Token);
                return ip == "" || ip.Contains(" ")
                    ? "공인 IP를 가져오지 못했습니다. 인터넷 연결을 확인하세요.\n\n" + ip
                    : "공인 IP (인터넷에서 보이는 이 PC의 주소)\n\n" + ip +
                      "\n\n위의 [IP 확인]에 나오는 192.168.x.x 같은 주소는 집이나 회사 안에서만 쓰는 주소입니다.";
            }));
    }

    public static Page Ping()
    {
        return new CommandPage("인터넷 연결 확인", "인터넷이 되는지 확인하고, 안 되면 IP를 다시 받거나 설정을 초기화합니다.", "ping google.com",
            "연결 확인", (CommandPage.Action)(async p =>
            {
                p.Append("[ 8.8.8.8 (인터넷 연결) ]");
                await p.Run("ping", "-n 4 8.8.8.8");
                p.Append("");
                p.Append("[ google.com (DNS 포함) ]");
                await p.Run("ping", "-n 4 google.com");
                return null;
            }),
            "경로 추적", Live("tracert", "-d -h 15 8.8.8.8"),
            "IP 다시 받기", (CommandPage.Action)(p =>
            {
                if (!Shell.Confirm("공유기에서 IP를 다시 받습니다.\n인터넷이 몇 초 동안 끊깁니다. 계속할까요?")) return Done(null);
                return Done(Shell.RunAdmin("ipconfig /release & ipconfig /renew")
                    ? "관리자 권한 cmd 창에서 IP를 다시 받고 있습니다.\n결과는 새로 열린 창에서 확인하세요."
                    : "관리자 권한 요청이 취소되었습니다.");
            }),
            "네트워크 초기화", (CommandPage.Action)(p =>
            {
                if (!Shell.Confirm("네트워크 설정(Winsock, TCP/IP)을 초기화합니다.\n" +
                                   "다른 방법으로 해결되지 않을 때만 쓰세요. 직접 넣은 고정 IP나 VPN 설정이 사라질 수 있고,\n" +
                                   "끝난 뒤 PC를 재시작해야 적용됩니다. 계속할까요?")) return Done(null);
                return Done(Shell.RunAdmin("netsh winsock reset & netsh int ip reset & ipconfig /flushdns & echo. & echo 끝났습니다. PC를 재시작하세요.")
                    ? "관리자 권한 cmd 창에서 초기화를 시작했습니다.\n끝나면 PC를 재시작하세요."
                    : "관리자 권한 요청이 취소되었습니다.");
            }));
    }

    static Task<string> Done(string text) { return Task.FromResult(text); }

    public static Page Dns()
    {
        return new CommandPage("DNS 캐시 비우기", "사이트가 갑자기 안 열리거나 주소가 바뀐 사이트에 접속이 안 될 때 사용합니다.", "ipconfig /flushdns",
            "DNS 비우기", (CommandPage.Action)(async p =>
            {
                int code = await Shell.RunAdminHidden("ipconfig", "/flushdns"); // 관리자 권한이 있어야 함
                return code == 0 ? "DNS 확인자 캐시를 비웠습니다."
                     : code == -1 ? "관리자 권한 요청이 취소되었습니다."
                     : "실패했습니다. (코드 " + code + ")";
            }),
            "캐시 보기", Live("ipconfig", "/displaydns"));
    }

    public static Page Wifi()
    {
        var page = new CommandPage("와이파이 비밀번호", "이 PC에 저장된 와이파이 이름과 비밀번호를 보여줍니다.", "netsh wlan show profile key=clear",
            "비밀번호 보기", (CommandPage.Action)(async p =>
            {
                string list = await Shell.Capture("netsh", "wlan show profiles", p.Token);
                var names = new List<string>();
                foreach (string line in list.Split('\n'))
                    if ((line.Contains("All User Profile") || line.Contains("모든 사용자 프로필")) && line.Contains(":"))
                        names.Add(line.Substring(line.IndexOf(':') + 1).Trim());
                if (names.Count == 0) return "저장된 와이파이를 찾지 못했습니다.\n\n" + list;

                p.Append("저장된 와이파이 " + names.Count + "개를 확인하는 중...");
                // 한 번에 같이 조회
                string[] infos = await Task.WhenAll(names.ConvertAll(n =>
                    Shell.Capture("netsh", "wlan show profile name=\"" + n + "\" key=clear", p.Token)));

                var sb = new StringBuilder();
                for (int i = 0; i < names.Count; i++)
                {
                    string key = "(비밀번호 없음 또는 확인 불가)";
                    foreach (string line in infos[i].Split('\n'))
                        if ((line.Contains("Key Content") || line.Contains("키 콘텐츠")) && line.Contains(":"))
                            key = line.Substring(line.IndexOf(':') + 1).Trim();
                    sb.AppendLine(names[i]);
                    sb.AppendLine("    " + key);
                    sb.AppendLine();
                }
                return sb.ToString();
            }));
        page.AddCopyButton();
        return page;
    }

    public static Page Temp()
    {
        string temp = Path.GetTempPath();
        return new CommandPage("디스크 정리", "임시 파일, 휴지통, 최대 절전 파일을 지워 C 드라이브 용량을 확보합니다.", "del /s /q %TEMP%\\*",
            "임시 파일 정리", (CommandPage.Action)(async p =>
            {
                if (!Shell.Confirm("임시 폴더에서 하루 넘게 지난 파일을 삭제할까요?\n" + temp)) return "취소했습니다.";
                return await Task.Run(() =>
                {
                    long bytes = 0; int deleted = 0, recent = 0, skipped = 0;
                    Clean(temp, false, DateTime.Now.AddDays(-1), ref bytes, ref deleted, ref recent, ref skipped);
                    return string.Format("정리 완료\n\n삭제한 파일: {0}개\n확보한 용량: {1:0.0} MB\n" +
                                         "최근 24시간 안에 쓴 파일이라 남겨둠: {2}개\n사용 중이거나 권한이 없어 건너뜀: {3}개",
                        deleted, bytes / 1048576.0, recent, skipped);
                });
            }),
            "휴지통 비우기", (CommandPage.Action)(p => Done(EmptyRecycleBin())),
            "최대 절전 끄기", (CommandPage.Action)(async p =>
            {
                var hiber = new FileInfo(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "hiberfil.sys"));
                if (!hiber.Exists) return "최대 절전 모드가 이미 꺼져 있습니다. (hiberfil.sys 없음)";
                if (!Shell.Confirm(string.Format("최대 절전 모드를 끄고 hiberfil.sys({0:0.0} GB)를 지울까요?\n\n" +
                    "빠른 시작(Fast Startup)도 함께 꺼져서 부팅이 조금 느려질 수 있습니다.\n" +
                    "다시 켜려면 관리자 cmd에서 powercfg /h on 을 실행하세요.", hiber.Length / 1073741824.0))) return null;
                int code = await Shell.RunAdminHidden("powercfg", "/h off");
                return code == 0 ? "최대 절전 모드를 껐습니다. hiberfil.sys가 지워졌습니다."
                     : code == -1 ? "관리자 권한 요청이 취소되었습니다."
                     : "실패했습니다. (코드 " + code + ")";
            }),
            "폴더 열기", (CommandPage.Action)(p => { Shell.Open(temp); return Done(null); }));
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SHQUERYRBINFO { public int cbSize; public long i64Size; public long i64NumItems; }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHQueryRecycleBin(string root, ref SHQUERYRBINFO info);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHEmptyRecycleBin(IntPtr hwnd, string root, uint flags);

    static string EmptyRecycleBin()
    {
        var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO)) };
        SHQueryRecycleBin(null, ref info);
        if (info.i64NumItems == 0) return "휴지통이 이미 비어 있습니다.";
        string what = string.Format("{0}개, {1:0.0} MB", info.i64NumItems, info.i64Size / 1048576.0);
        if (!Shell.Confirm("휴지통의 항목 " + what + "를 영구 삭제할까요?\n삭제하면 되돌릴 수 없습니다.")) return null;
        const uint NoConfirm = 0x1, NoProgress = 0x2, NoSound = 0x4;
        int hr = SHEmptyRecycleBin(IntPtr.Zero, null, NoConfirm | NoProgress | NoSound);
        return hr == 0 ? "휴지통을 비웠습니다. (" + what + ")" : "휴지통을 비우지 못했습니다. (코드 0x" + hr.ToString("X8") + ")";
    }

    static void Clean(string dir, bool removeSelf, DateTime cutoff, ref long bytes, ref int deleted, ref int recent, ref int skipped)
    {
        try
        {
            foreach (string f in Directory.GetFiles(dir))
            {
                try
                {
                    var fi = new FileInfo(f);
                    // 설치 프로그램 등이 지금 쓰고 있을 수 있는 최근 파일은 남김
                    if (fi.LastWriteTime > cutoff || fi.CreationTime > cutoff) { recent++; continue; }
                    long len = fi.Length;
                    fi.Delete();
                    bytes += len;
                    deleted++;
                }
                catch { skipped++; }
            }
            foreach (string d in Directory.GetDirectories(dir))
            {
                // 링크(정션)는 따라 들어가지 않음: 임시 폴더 밖의 파일이 지워지는 것을 막음
                if ((File.GetAttributes(d) & FileAttributes.ReparsePoint) != 0) { skipped++; continue; }
                Clean(d, true, cutoff, ref bytes, ref deleted, ref recent, ref skipped);
            }
            if (removeSelf && Directory.GetFileSystemEntries(dir).Length == 0) Directory.Delete(dir);
        }
        catch { }
    }

    public static Page SystemInfo()
    {
        return new CommandPage("시스템 정보", "Windows 버전, 메모리, 디스크 용량을 보고, 시계를 맞추거나 파일 해시를 확인합니다.", "systeminfo",
            "정보 보기", Live("systeminfo", ""),
            "디스크 용량", (CommandPage.Action)(p => Done(DiskInfo())),
            "시간 맞추기", (CommandPage.Action)(p => Done(Shell.RunAdmin("net start w32time & w32tm /resync")
                ? "관리자 권한 cmd 창에서 인터넷 시간 서버와 시계를 맞추고 있습니다.\n결과는 새로 열린 창에서 확인하세요."
                : "관리자 권한 요청이 취소되었습니다.")),
            "파일 해시", (CommandPage.Action)(async p =>
            {
                string file;
                using (var dlg = new OpenFileDialog { Title = "해시를 확인할 파일 선택" })
                {
                    if (dlg.ShowDialog() != DialogResult.OK) return null;
                    file = dlg.FileName;
                }
                p.Append("파일: " + file);
                p.Append("내려받은 사이트에 적힌 SHA256 값과 같으면 파일이 손상되거나 바뀌지 않은 것입니다.");
                p.Append("");
                await p.Run("certutil", "-hashfile \"" + file + "\" SHA256");
                return null;
            }));
    }

    static string DiskInfo()
    {
        var sb = new StringBuilder();
        foreach (var d in DriveInfo.GetDrives())
        {
            if (!d.IsReady || d.TotalSize == 0) continue;
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
                try { File.Delete(path); } catch { }
                await p.Run("powercfg", "/batteryreport /output \"" + path + "\"");
                if (File.Exists(path)) Shell.Open(path);
                return null;
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
                : "관리자 권한 요청이 취소되었습니다.")),
            "디스크 검사", (CommandPage.Action)(p => Task.FromResult(Shell.RunAdmin("chkdsk C: /scan")
                ? "관리자 권한 cmd 창에서 C 드라이브 오류 검사를 시작했습니다. (검사만 하고 고치지는 않습니다)\n" +
                  "오류가 나오면 관리자 cmd에서 chkdsk C: /f 를 실행하고 재시작하면 고칩니다."
                : "관리자 권한 요청이 취소되었습니다.")));
    }

    // 포트 번호 또는 프로그램 이름으로 찾고 강제 종료
    public static Page Ports()
    {
        var pids = new List<int>();
        TextBox input = null;
        var page = new CommandPage("포트·프로세스", "포트 번호나 프로그램 이름으로 실행 중인 프로그램을 찾아 강제로 끕니다.", "netstat -ano  →  taskkill /F /PID",
            "찾기", (CommandPage.Action)(async p =>
            {
                string q = input.Text.Trim();
                pids.Clear();
                if (q == "") return "왼쪽 칸에 포트 번호(예: 3000) 또는 프로그램 이름(예: chrome)을 넣고 [찾기]를 누르세요.";
                int port;
                if (int.TryParse(q, out port)) return FindPort(await Shell.Capture("netstat", "-ano", p.Token), port, pids);
                return FindProcess(q, pids);
            }),
            "강제 종료", (CommandPage.Action)(async p =>
            {
                if (pids.Count == 0) return "먼저 [찾기]로 끌 프로그램을 찾으세요.";
                if (!Shell.Confirm("찾은 프로그램 " + pids.Count + "개를 강제로 끌까요?\n저장하지 않은 작업은 사라집니다.\n\nPID: " + string.Join(", ", pids))) return null;
                string r = await Shell.Capture("taskkill", "/F /T /PID " + string.Join(" /PID ", pids), p.Token);
                pids.Clear();
                return r + "\n\n(\"액세스가 거부되었습니다\"가 나오면 관리자 권한으로 실행 중인 프로그램이라 끌 수 없습니다)";
            }));
        input = page.AddInput("포트 또는 이름");
        return page;
    }

    static string ProcName(int pid)
    {
        try { using (var pr = Process.GetProcessById(pid)) return pr.ProcessName; }
        catch { return "?"; }
    }

    static string FindPort(string netstat, int port, List<int> pids)
    {
        var sb = new StringBuilder();
        foreach (string raw in netstat.Split('\n'))
        {
            string[] c = raw.Split(new[] { ' ', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (c.Length < 4 || (c[0] != "TCP" && c[0] != "UDP")) continue;
            if (!c[1].EndsWith(":" + port)) continue; // 이 PC 쪽 주소가 그 포트인 것만
            int pid;
            if (!int.TryParse(c[c.Length - 1], out pid)) continue;
            string state = c.Length > 4 ? string.Join(" ", c, 3, c.Length - 4) : "";
            sb.AppendLine(string.Format("{0,-4} {1,-22} {2,-12} {3,7}  {4}", c[0], c[1], state, pid, ProcName(pid)));
            if (pid > 4 && !pids.Contains(pid)) pids.Add(pid); // 0, 4는 Windows 자체
        }
        if (sb.Length == 0) return port + "번 포트를 쓰는 프로그램이 없습니다.";
        return string.Format("{0,-4} {1,-22} {2,-12} {3,7}  {4}\n", "종류", "주소", "상태", "PID", "프로그램") + sb +
               (pids.Count > 0 ? "\n[강제 종료]를 누르면 위 프로그램을 끕니다." : "\nWindows 시스템이 쓰는 포트라 끌 수 없습니다.");
    }

    static string FindProcess(string name, List<int> pids)
    {
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - 4);
        var found = new List<Process>();
        foreach (var pr in Process.GetProcesses())
        {
            bool match = false;
            try { match = pr.Id > 4 && pr.ProcessName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0; } catch { } // 그 사이 꺼진 경우
            if (match) found.Add(pr); else pr.Dispose();
        }
        if (found.Count == 0) return "'" + name + "'(이)가 들어간 프로그램이 실행 중이지 않습니다.";
        found.Sort((a, b) => string.Compare(a.ProcessName, b.ProcessName, StringComparison.OrdinalIgnoreCase));

        var sb = new StringBuilder();
        sb.AppendLine(string.Format("{0,7}  {1,-24} {2,9}  {3}", "PID", "프로그램", "메모리", "창 제목"));
        foreach (var pr in found)
        {
            string title = "";
            long mem = 0;
            try { title = pr.MainWindowTitle; mem = pr.WorkingSet64; } catch { }
            sb.AppendLine(string.Format("{0,7}  {1,-24} {2,7:0} MB  {3}", pr.Id, pr.ProcessName, mem / 1048576.0, title));
            pids.Add(pr.Id);
            pr.Dispose();
        }
        sb.AppendLine();
        sb.AppendLine(found.Count + "개를 찾았습니다. [강제 종료]를 누르면 모두 끕니다.");
        return sb.ToString();
    }

    public static Page Update()
    {
        return new CommandPage("프로그램 업데이트", "winget으로 새 버전이 나온 프로그램을 찾아 한 번에 업데이트합니다.", "winget upgrade --all",
            "업데이트 확인", Live("winget", "upgrade --accept-source-agreements --disable-interactivity"),
            "모두 업데이트", (CommandPage.Action)(async p =>
            {
                if (!Shell.Confirm("새 버전이 있는 프로그램을 모두 업데이트할까요?\n" +
                                   "업데이트하는 프로그램은 실행 중이면 꺼질 수 있고, 중간에 관리자 권한 창이 뜰 수 있습니다.")) return null;
                await p.Run("winget", "upgrade --all --accept-source-agreements --accept-package-agreements --disable-interactivity");
                return null;
            }));
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

    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    MainForm()
    {
        Text = "명령어 도구함";
        Font = Theme.Body;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(Theme.S(820), Theme.S(560));
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
            Item("포트·프로세스", Features.Ports),
            Item("디스크 정리", Features.Temp),
            Item("프로그램 업데이트", Features.Update),
            Item("시스템 정보", Features.SystemInfo),
            Item("배터리 리포트", Features.Battery),
            Item("시스템 파일 검사", Features.Sfc),
            Item("Windows 도구", Features.Tools),
        };

        for (int i = 0; i < list.Count; i++)
        {
            var nav = new NavItem { Number = i + 1, Text = list[i].Key, Location = new Point(10, 82 + i * 36), Size = new Size(200, 34) };
            var make = list[i].Value;
            nav.Click += (s, e) => Select(nav, make);
            navs.Add(nav);
            side.Controls.Add(nav);
        }

        // 100% 기준으로 배치한 위치와 크기를 화면 배율에 맞게 키움 (글자는 Windows가 알아서 키움)
        ScaleToScreen(side);
        ScaleToScreen(content);
        Select(navs[0], list[0].Value);
    }

    static void ScaleToScreen(Control c)
    {
        if (Theme.Factor != 1f) c.Scale(new SizeF(Theme.Factor, Theme.Factor));
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
            ScaleToScreen(page);
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
        // 화면 배율이 100%가 아니어도 흐리게 늘어나지 않고 선명하게 그리도록
        try { SetProcessDPIAware(); } catch { }
        using (var g = Graphics.FromHwnd(IntPtr.Zero)) Theme.Factor = g.DpiX / 96f;

        ShutdownPlan.Load();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
