Imports System
Imports System.IO

' https://github.com/lemnos/chrome-session-dump/blob/master/chrome-session-dump.go


Module Program



	'    package main

	'//Written by Aetnaeus
	'//Original Source: https://github.com/lemnos/chrome-session-dump

	'import (
	'	"bytes"
	'	"encoding/json"
	'	"flag"
	'	"fmt"
	'	"io"
	'	"io/ioutil"
	'	"os"
	'	"path"
	'	"sort"
	'	"strings"
	'	"unicode/utf16"
	')

	'//Rather than storing session state directly chrome appends a pickled command to a
	'//session file as tabs are manipulated. These commands are subsequently used to
	'//reconstruct the session when the browser Is restarted. Thus obtaining the
	'//working tab set involves attemping to simulate the reconstruction process
	'//performed by chrome (which Is an implementation detail liable to change).

	'//A file has the following format

	'//"SNSS"
	'//int32 version number (should be 1)
	'//<command>...

	'//Where each command has the following format

	'//<int16(size)><int8(type id)><payload (size-1 bytes)>...

	'//Where payload Is a pickled struct containing data of the
	'//given type.

	'//See https//source.chromium.org/chromium/chromium/src/+/master:components/sessions/core/session_service_commands.cc;bpv=1;bpt=1?q=kCommandUpdateTabNavigation&ss=chromium%2Fchromium%2Fsrc

	'//Source
	'//https//source.chromium.org/chromium/chromium/src/+/master:components/sessions/core/session_service_commands.cc;drc=948de71be4a38bc27197146904266867c509f4c0;bpv=1;bpt=1;l=25

	'Const (
	'	kCommandUpdateTabNavigation        = 6
	'	kCommandSetSelectedTabInIndex      = 8
	'	kCommandSetTabWindow               = 0
	'	kCommandSetTabGroup                = 25
	'	kCommandSetTabGroupMetadata2       = 27
	'	kCommandSetSelectedNavigationIndex = 7
	'	kCommandTabClosed                  = 16
	'	kCommandWindowClosed               = 17
	'	kCommandSetTabIndexInWindow        = 2
	'	kCommandSetActiveWindow            = 20
	'	kCommandLastActiveTime             = 21
	')

	'type group struct {
	'	high uint64
	'	low  uint64
	'	name string
	'}

	'type window struct {
	'	activeTabIdx uint32
	'	id           uint32
	'	deleted      bool
	'	tabs         []*tab
	'}

	'type histItem struct {
	'	idx   uint32
	'	url   string
	'	title string
	'}

	'type tab struct {
	'	id                uint32
	'	history           []*histItem
	'	idx               uint32 //The tab position In the window (a relative value)
	'	win               uint32 //the id Of the window To which the tab belongs
	'	deleted           bool
	'	currentHistoryIdx uint32
	'	group             *group //May be null
	'}

	'//indexed by id
	'var tabs = map[uint32]*tab{}
	'var windows = map[uint32]*window{}
	'var groups = map[string]*group{}

	'func getWindow(id uint32) *window {
	'	If _, ok := windows[id]; !ok {
	'		windows[id] = & window{id: id}
	'	}

	'	Return windows[id]
	'}

	'func getGroup(high uint64, low uint64) *group {
	'	key := fmt.Sprintf("%x%x", high, low)
	'	If _, ok := groups[key]; !ok {
	'		groups[key] = & group{high, low, ""}
	'	}

	'	Return groups[key]
	'}

	'func getTab(id uint32) *tab {
	'	If _, ok := tabs[id]; !ok {
	'		tabs[id] = & tab{id: id}
	'	}

	'	Return tabs[id]
	'}

	'func readUint8(r io.Reader) uint8 {
	'	var b [1]Byte
	'	If n, err := r.Read(b[:]); err != nil || n != 1 {
	'		If err!= nil {
	'			panic(err)
	'		}
	'		panic(fmt.Errorf("Failed to read int8."))
	'	}

	'	Return uint8(b[0])
	'}

	'func readUint16(r io.Reader) uint16 {
	'	var b [2]Byte
	'	If n, err := r.Read(b[:]); err != nil || n != 2 {
	'		If err!= nil {
	'			panic(err)
	'		}
	'		panic(fmt.Errorf("Failed to read int16."))
	'	}

	'	Return uint16(b[0]) | uint16(b[1])<<8
	'}

	'func readUint32(r io.Reader) uint32 {
	'	var b [4]Byte
	'	If n, err := r.Read(b[:]); err != nil || n != 4 {
	'		If err!= nil {
	'			panic(err)
	'		}

	'		panic(fmt.Errorf("Failed to read uint32."))
	'	}

	'	Return uint32(b[3]) << 24 | uint32(b[2])<<16 | uint32(b[1])<<8 | uint32(b[0])
	'}

	'func readUint64(r io.Reader) uint64 {
	'	var b [8]Byte
	'	If n, err := r.Read(b[:]); err != nil || n != 8 {
	'		If err!= nil {
	'			panic(err)
	'		}

	'		panic(fmt.Errorf("Failed to read uint64."))
	'	}

	'	Return uint64(b[7]) << 56 |
	'		uint64(b[6])<<48 |
	'		uint64(b[5])<<40 |
	'		uint64(b[4])<<32 |
	'		uint64(b[3])<<24 |
	'		uint64(b[2])<<16 |
	'		uint64(b[1])<<8 |
	'		uint64(b[0])
	'}

	'func readString(r io.Reader) string {
	'	sz := readUint32(r)
	'	rsz := sz
	'	If rsz%4 != 0 { //Chrome 32bit aligns pickled data
	'		rsz += 4 - (rsz % 4)
	'	}

	'	b := make([]byte, rsz)

	'	If n, err := io.ReadFull(r, b); err != nil {
	'		panic(err)
	'	} else if n != len(b) {
	'		panic(fmt.Errorf("Failed to read string"))
	'	}

	'	Return String(b[: sz]) //don't return padding
	'}

	'//Reads a pickled 16 bit string NOTE: chrome appears To store the internal c++ std representation
	'//which Is Not guaranteed to be UTF16 And may vary by platform And locale (aka this works on
	'//MY machine :P).

	'func readString16(r io.Reader) string {
	'	sz := readUint32(r)
	'	rsz := sz * 2
	'	If rsz%4 != 0 { //Chrome 32bit aligns pickled data
	'		rsz += 4 - (rsz % 4)
	'	}

	'	b := make([]byte, rsz)

	'	If n, err := io.ReadFull(r, b); err != nil {
	'		panic(err)
	'	} else if n != len(b) {
	'		panic(fmt.Errorf("Failed to read string"))
	'	}

	'	var s []uint16
	'	For i := 0; i < int(sz*2); i += 2 {
	'		s = append(s, uint16(b[i+1])<<8|uint16(b[i]))
	'	}

	'	Return String(utf16.Decode(s))
	'}

	'//Normalized output structures (as distinct from the lower case internal ones which correspond to SNSS structures)

	'type Result struct {
	'	Windows []*Window `json:"windows"`
	'}

	'type Tab struct {
	'	Active  bool           `json:"active"`
	'	History []*HistoryItem `json:"history"`
	'	Url     string         `json:"url"`
	'	Title   string         `json:"title"`
	'	Deleted bool           `json:"deleted"`
	'	Group   string         `json:"group"`
	'}

	'type Window struct {
	'	Tabs    []*Tab `json:"tabs"`
	'	Active  bool   `json:"active"`
	'	Deleted bool   `json:"deleted"`
	'}

	'type HistoryItem struct {
	'	Url   string `json:"url"`
	'	Title string `json:"title"`
	'}

	'func parse(path string) Result {
	'	fh, err := os.Open(path)
	'	If err!= nil {
	'		panic(err)
	'	}

	'	var magic [4]Byte

	'	If n, err := fh.Read(magic[:4]); err != nil || n != 4 {
	'		panic(err)
	'	}

	'	ver := readUint32(fh)

	'	If magic!= [4]byte{0x53, 0x4E, 0x53, 0x53} || //0x534E5353 == "SNSS"
	'		(ver != 1 && ver != 3) { //TODO (hotfix): Review https : //source.chromium.org/chromium/chromium/src/+/807acce36a4baa1004d23ae896b07e2148ea1533 And implement neccesary changes.

	'		panic(fmt.Errorf("Invalid SNSS file: (version %d)", ver))
	'	}

	'	var activeWindow *window

	'	readCommand := func() (typ uint8, data io.Reader, eof bool) {
	'		defer func() {
	'			If e := recover(); e == io.EOF {
	'				eof = true
	'				Return
	'			} else if e != nil {
	'				panic(err)
	'			}
	'		}()

	'		sz := int(readUint16(fh)) - 1

	'		typ = readUint8(fh)
	'		buf := make([]byte, sz)

	'		If n, err := fh.Read(buf); err != nil {
	'			panic(err)
	'		} else if n != sz {
	'			panic(fmt.Errorf("Failed to read %d bytes", n))
	'		}

	'		Return typ, bytes.NewBuffer(buf), False
	'	}

	'	For {
	'		typ, data, eof := readCommand()
	'		If eof {
	'			break
	'		}

	'		//Note: Some commands are pickled whilst others are raw struct
	'		//dumps from memory, the former have a 32 bit size header whilst the
	'		//latter may include padding between members.

	'		switch typ {
	'		Case kCommandUpdateTabNavigation : 
	'			readUint32(data) //size of the data (again)

	'			id := readUint32(data)
	'			histIdx := readUint32(data)
	'			url := readString(data)
	'			title := readString16(data)

	'			t := getTab(id)

	'			var item *histItem
	'			For _, h := range t.history {
	'				If h.idx == histIdx {
	'					item = h
	'					break
	'				}
	'			}

	'			If item == nil {
	'				item = &histItem{idx: histIdx}
	'				t.history = append(t.history, item)
	'			}

	'			item.url = url
	'			item.title = title
	'		Case kCommandSetSelectedTabInIndex :  //Sets the active tab index in window, note that 'tab index' is a derived value and not present in any data.
	'			id := readUint32(data)
	'			idx := readUint32(data)

	'			getWindow(id).activeTabIdx = idx
	'		Case kCommandSetTabGroupMetadata2 : 
	'			readUint32(data) //Size

	'			high := readUint64(data)
	'			low := readUint64(data)

	'			name := readString16(data)
	'			getGroup(high, low).name = name
	'		Case kCommandSetTabGroup : 
	'			id := readUint32(data)
	'			readUint32(data) //Struct padding

	'			high := readUint64(data)
	'			low := readUint64(data)

	'			getTab(id).group = getGroup(high, low)
	'		Case kCommandSetTabWindow : 
	'			win := readUint32(data)
	'			id := readUint32(data)

	'			getTab(id).win = win
	'		Case kCommandWindowClosed : 
	'			id := readUint32(data)

	'			getWindow(id).deleted = true
	'		Case kCommandTabClosed : 
	'			id := readUint32(data)

	'			getTab(id).deleted = true
	'		Case kCommandSetTabIndexInWindow : 
	'			id := readUint32(data)
	'			index := readUint32(data)

	'			getTab(id).idx = index
	'		Case kCommandSetActiveWindow : 
	'			id := readUint32(data)

	'			activeWindow = getWindow(id)
	'		Case kCommandLastActiveTime :  //TODO implement properly
	'			//id := readUint32(data)
	'			//time := readUint64(data)

	'			//getTab(id)._lastActiveTime = time //figure out how to interpret this.
	'		Case kCommandSetSelectedNavigationIndex : 
	'			id := readUint32(data)
	'			idx := readUint32(data) //The current position within history

	'			getTab(id).currentHistoryIdx = idx
	'		}
	'	}

	'	For _, t := range tabs {
	'		sort.Slice(t.history, func(i, j int) bool {
	'			Return t.history[i].idx < t.history[j].idx
	'		})

	'		w := getWindow(t.win)
	'		w.tabs = append(w.tabs, t)
	'	}

	'	For _, w := range windows {
	'		sort.Slice(w.tabs, func(i, j int) bool {
	'			Return w.tabs[i].idx < w.tabs[j].idx
	'		})
	'	}

	'	var Windows []*Window

	'	For _, w := range windows {
	'		W := &Window{Active: w == activeWindow, Deleted: w.deleted}

	'		idx := 0
	'		For _, t := range w.tabs {
	'			groupName := ""
	'			If t.group!= nil {
	'				groupName = t.group.name
	'			}

	'			T := &Tab{Active idx == int(w.activeTabIdx), Deleted: t.deleted, Group: groupName}

	'			For _, h := range t.history {
	'				T.History = append(T.History, &HistoryItem{h.url, h.title})
	'				If h.idx == t.currentHistoryIdx { //Truncate history To avoid having To deal With trees TODO: find a better way To export this.
	'					T.Url = h.url
	'					T.Title = h.title
	'					break
	'				}
	'			}

	'			W.Tabs = append(W.Tabs, T)
	'			If !t.deleted {
	'				idx++
	'			}
	'		}

	'		Windows = append(Windows, W)
	'	}

	'	Return Result{Windows}
	'}

	'func findSession(_path string) string {
	'	var cfile = ""

	'	ents, err := ioutil.ReadDir(_path)
	'	If err!= nil {
	'		panic(err)
	'	}

	'	cmp := func(candidate string) {
	'		If cfile!= "" {
	'			info1, err := os.Stat(candidate)
	'			If err!= nil {
	'				panic(err)
	'			}

	'			info2, err := os.Stat(cfile)
	'			If err!= nil {
	'				panic(err)
	'			}
	'			If info1.ModTime().Sub(info2.ModTime()) > 0 {
	'				cfile = candidate
	'			}
	'		} else {
	'			cfile = candidate
	'		}
	'	}

	'	For _, ent := range ents {
	'		If ent.IsDir() {
	'			If cand := findSession(path.Join(_path, ent.Name())); cand != "" {
	'				cmp(cand)
	'			}
	'		} else if strings.Index(ent.Name(), "Session_") == 0 {
	'			cmp(path.Join(_path, ent.Name()))
	'		}
	'	}

	'	Return cfile
	'}

	'func tabPrintf(Format string, tab *Tab, includeHistory bool) {
	'	If includeHistory {
	'		For _, item := range tab.History {
	'			s := strings.Replace(format, "%u", item.Url, -1)
	'			s = strings.Replace(s, "%g", tab.Group, -1)
	'			s = strings.Replace(s, "%t", item.Title, -1)
	'			s = strings.Replace(s, "\\n", "\n", -1)
	'			s = strings.Replace(s, "\\t", "\t", -1)
	'			s = strings.Replace(s, "\\0", "\x00", -1)

	'			os.Stdout.Write([]byte(s))
	'		}
	'	} else {
	'		s := strings.Replace(format, "%u", tab.Url, -1)
	'		s = strings.Replace(s, "%g", tab.Group, -1)
	'		s = strings.Replace(s, "%t", tab.Title, -1)
	'		s = strings.Replace(s, "\\n", "\n", -1)
	'		s = strings.Replace(s, "\\t", "\t", -1)
	'		s = strings.Replace(s, "\\0", "\x00", -1)

	'		os.Stdout.Write([]byte(s))
	'	}
	'}

	Sub Main(args As String())



		'var jsonFlag bool
		'var activeFlag bool
		'var deletedFlag bool
		'var historyFlag bool
		'var outputFmt String

		'flag.BoolVar(&jsonFlag, "json", false, "Produce json formatted output. Note that this includes all tabs along with their history and any corresponding metadata. Useful for other scripts.")
		'flag.BoolVar(&activeFlag, "active", false, "Print the currently active tab.")
		'flag.StringVar(&outputFmt, "printf", "%u\n", "The output format for tabs if -json is not specified (%u = url, %t = title, %g = group).")

		'flag.BoolVar(&deletedFlag, "deleted", false, "Include tabs which have been deleted.")
		'flag.BoolVar(&historyFlag, "history", false, "Include the history of each tab in the output.")

		'flag.Usage = func() {
		'	fmt.Printf("Usage: 
		'		   -session-dump [options] ([session file] | [chrome dir])\n\n")
		'	fmt.Printf(`If a chrome directory Is supplied the most recent session file contained within it Is used. If neither a directory Or file  Is supplied then the program will use ~/.config/chrome by Default `)

		'	flag.PrintDefaults()
		'}

		'flag.Parse()

		Dim target As String = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
		target = target & "\Microsoft\Edge\User Data\Default\Sync Data\LevelDB\"

		Dim lastlog As String = ""
		For Each plik In IO.Directory.GetFiles(target, "*.log")
			lastlog = plik
		Next

		If lastlog = "" Then
			Console.WriteLine("Nie znalazłem logu!")
			Return
		End If

		Console.WriteLine("skorzystam z pliku logu " & lastlog)

		'OpenRead ma Share.Read, i wtedy jest exception :)
		Dim strumyk As IO.FileStream = IO.File.Open(lastlog, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
		Dim rider = New StreamReader(strumyk)
		Dim content As String = rider.ReadToEnd
		rider.Dispose()
		strumyk.Close()

		' tak byc nie moze, bo sharing violation
		'Dim content As String = IO.File.ReadAllText(lastlog)
		Dim iInd As Integer = content.LastIndexOf("https://www.facebook.com/photo/?")
		content = content.Substring(iInd)

		Dim pageaddr As String = ""
		For iLp = 0 To content.Length
			Dim znak As Char = content.Chars(iLp)
			If znak < " " Or znak > "z" Then Exit For
			pageaddr &= znak
		Next

		Console.WriteLine("Ostatni URL to " & pageaddr)
		'		:= os.ExpandEnv("$HOME/.config/chromium")

		'	If _, err := os.Stat(target); os.IsNotExist(err) {
		'		target = os.ExpandEnv("$HOME/.config/google-chrome")
		'	}

		'	If _, err := os.Stat(target); os.IsNotExist(err) {
		'		target = os.ExpandEnv("$HOME/.config/chrome")
		'	}

		'	If len(flag.Args()) >= 1 {
		'		target = flag.Args()[0]
		'	}

		'	If info, err := os.Stat(target); err == nil && info.IsDir() {
		'		target = findSession(target)
		'	}

		'	If target == "" {
		'		panic(fmt.Errorf("Unable to find session file."))
		'	}

		'	data := parse(target)

		'	If jsonFlag {
		'		b, err := json.Marshal(data)
		'		If err!= nil {
		'			panic(err)
		'		}

		'		fmt.Println(string(b))
		'	} else if activeFlag {
		'		For _, win := range data.Windows {
		'			If win.Active {
		'				For _, tab := range win.Tabs {
		'					If tab.Active {
		'						tabPrintf(outputFmt, tab, historyFlag)
		'					}
		'				}
		'			}
		'		}
		'	} else {
		'		For _, win := range data.Windows {
		'			If deletedFlag || !win.Deleted {
		'				For _, tab := range win.Tabs {
		'					If deletedFlag || !tab.Deleted {
		'						tabPrintf(outputFmt, tab, historyFlag)
		'					}
		'				}
		'			}
		'		}
		'	}
		'}

	End Sub
End Module
