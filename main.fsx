#nowarn
#load "lwc.fsx"
#load "MyControls.fsx"

open Lwc
open MyControls
open System
open System.Drawing
open System.Windows.Forms

let padding = new Padding(10)
(*
    funzione per la creazione di un nuovo bottone, e assegnazione
    del bottobe ad un pannello
*)
let newButton(text:string, panel:FlowLayoutPanel) =
    let button = new Button(Text = text, Enabled=true, AutoSize=true, FlatStyle=FlatStyle.System, Margin = padding)
    panel.Controls.Add(button)
    button

    // funzione per la creazione di un nuovo pannello
let newPanel(label:string, color:Color) =
    let panel = new FlowLayoutPanel(Dock=DockStyle.Top, FlowDirection=FlowDirection.LeftToRight, BackColor=color, AutoSize=true)
    let l = new Label(Text = label, TextAlign = ContentAlignment.MiddleCenter, Margin = padding)
    panel.Controls.Add(l)
    panel

// finestra principale del programma
let f = new Form(Text = "MidTerm - Jacopo Massa", WindowState=FormWindowState.Maximized)

// container dei controlli grafici
let autodrome = new Autodrome(Dock = DockStyle.Fill, BackColor = Color.LightGoldenrodYellow)

//pannello con le trasformazioni del mondo
let transformWorldPanel = newPanel("Transform world: ", Color.LightSeaGreen)
let upBtnWorld= newButton("↑",transformWorldPanel)
upBtnWorld.Click.Add(fun _ -> autodrome.Translate(0.f, 10.f, true))
let downBtnWorld= newButton("↓",transformWorldPanel)
downBtnWorld.Click.Add(fun _ -> autodrome.Translate(0.f, -10.f, true))
let leftBtnWorld= newButton("←",transformWorldPanel)
leftBtnWorld.Click.Add(fun _ -> autodrome.Translate(10.f, 0.f, true))
let rightBtnWorld= newButton("→",transformWorldPanel)
rightBtnWorld.Click.Add(fun _ -> autodrome.Translate(-10.f, 0.f, true))
let zoomPlusBtnWorld = newButton("Zoom +",transformWorldPanel)
zoomPlusBtnWorld.Click.Add(fun _ -> autodrome.Zoom(1.f/1.1f, true))
let zoomMinBtnWorld = newButton("Zoom -",transformWorldPanel)
zoomMinBtnWorld.Click.Add(fun _ -> autodrome.Zoom(1.1f, true))
let rotatePlusBtnWorld = newButton("Rotate ↶",transformWorldPanel)
rotatePlusBtnWorld.Click.Add(fun _ -> autodrome.Rotate(10.f, true))
let rotateMinBtnWorld = newButton("Rotate ↷",transformWorldPanel)
rotateMinBtnWorld.Click.Add(fun _ -> autodrome.Rotate(-10.f, true))

//pannello con le trasformazioni della vista
let transformViewPanel = newPanel("Transform view: ", Color.LimeGreen)
let upBtnView= newButton("↑",transformViewPanel)
upBtnView.Click.Add(fun _ -> autodrome.Translate(0.f, -10.f, false))
let downBtnView= newButton("↓",transformViewPanel)
downBtnView.Click.Add(fun _ -> autodrome.Translate(0.f, 10.f, false))
let leftBtnView= newButton("←",transformViewPanel)
leftBtnView.Click.Add(fun _ -> autodrome.Translate(-10.f, 0.f, false))
let rightBtnView= newButton("→",transformViewPanel)
rightBtnView.Click.Add(fun _ -> autodrome.Translate(10.f, 0.f, false))
let zoomPlusBtnView = newButton("Zoom +",transformViewPanel)
zoomPlusBtnView.Click.Add(fun _ -> autodrome.Zoom(1.f/1.1f, false))
let zoomMinBtnView = newButton("Zoom -",transformViewPanel)
zoomMinBtnView.Click.Add(fun _ -> autodrome.Zoom(1.1f, false))
let rotatePlusBtnView = newButton("Rotate ↶",transformViewPanel)
rotatePlusBtnView.Click.Add(fun _ -> autodrome.Rotate(10.f, false))
let rotateMinBtnView = newButton("Rotate ↷",transformViewPanel)
rotateMinBtnView.Click.Add(fun _ -> autodrome.Rotate(-10.f, false))

//pannello con le azioni da fare su un tracciato
let actionPanel = newPanel("Actions: ", Color.LightGreen)
let newRouteBtn = newButton("New Route(s)", actionPanel)
newRouteBtn.Click.Add(fun _ -> autodrome.Operation <- "NEW")
let selectRouteBtn = newButton("Select/Unselect Route(s)", actionPanel)
selectRouteBtn.Click.Add(fun _ -> autodrome.Operation <- "SELECT")
let moveRouteBtn = newButton("Move Route", actionPanel)
moveRouteBtn.Click.Add(fun _ -> autodrome.Operation <- "MOVE")
let deleteRouteBtn = newButton("Delete Route(s)", actionPanel)
deleteRouteBtn.Click.Add(fun _ -> autodrome.DeleteRoutes)
let animateRouteBtn = newButton("Animate Route(s)", actionPanel)
animateRouteBtn.Click.Add(fun _ -> autodrome.AnimateRoutes)
let stopAnimateRouteBtn = newButton("Stop Animation(s)", actionPanel)
stopAnimateRouteBtn.Click.Add(fun _ -> autodrome.StopAnimateRoutes)

animateRouteBtn.Click.Add(fun _ ->
    if(autodrome.SelectedRoutes.Count <> 0) then
        stopAnimateRouteBtn.Enabled <- true)

autodrome.Select()
f.Controls.Add(autodrome)
f.Controls.Add(actionPanel)
f.Controls.Add(transformViewPanel)
f.Controls.Add(transformWorldPanel)
f.MinimumSize <- Size(900, 800)
f.Show()
f.SizeChanged.Add(fun _ -> autodrome.Center <- PointF(single autodrome.Width/2.f, single autodrome.Height/2.f))
autodrome.Center <- PointF(single autodrome.Width/2.f, single autodrome.Height/2.f)