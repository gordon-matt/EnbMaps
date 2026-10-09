window.onload = GroesseAnPHP;
window.onresize = GroesseAnPHP;

// Hoehe und Breite


// Variablen für die Navs

var StarMap = self;
var InfoBox = null;

// Variablen für die Infobox
var Rand = 5;					// Abstand der Infobox zum Browserrand
var offsetX = 10;
var offsetY = 10;
var mouseX = 0;
var mouseY = 0;
var FolgeMaus = 0;
var realeHoehe = 0;
var realeBreite = 0;

// Initialisierung

var ns = (navigator.appName=='Netscape' && parseInt(navigator.appVersion) == 4);
var mz = (document.getElementById) ? true : false;
var ie = (document.all) ? true : false;
var ie5 = false;
if (ie) var docRoot = 'document.body';

if (ns) {
	var oW = window.innerWidth;
	var oH = window.innerHeight;
	window.onresize = function () {
		if (oW!=window.innerWidth||oH!=window.innerHeight) location.reload(); 
	}
}

if (ie) {
	if ((navigator.userAgent.indexOf('MSIE 5') > 0) || (navigator.userAgent.indexOf('MSIE 6') > 0)) {
		if(document.compatMode && document.compatMode == 'CSS1Compat') docRoot = 'document.documentElement';
		ie5 = true;
	}
	if (mz) {
		mz = false;
	}
}

if ( (ns) || (ie) || (mz)) {
	document.onmousemove = mouseMove
	if (ns) {
		document.captureEvents(Event.MOUSEMOVE)
		InfoBox = StarMap.document.mouseoverbox;
	}
	if (ie) {
		InfoBox = StarMap.mouseoverbox.style;
	}
	if (mz) {
		InfoBox = StarMap.document.getElementById("mouseoverbox");
	}
} else {
	InfoAn = Fake;
	InfoAus = Fake;
}

// Funktionen

function GroesseAnPHP() {
	var MSBreite = (document.documentElement.clientWidth == 0)?document.body.clientWidth:document.documentElement.clientWidth;
	var Breite = (window.innerWidth == undefined)?MSBreite:window.innerWidth;
	var MSHoehe = (document.documentElement.clientHeight == 0)?document.body.clientHeight:document.documentElement.clientHeight;
	var Hoehe = (window.innerHeight == undefined)?MSHoehe:window.innerHeight;
	var AlteBreite = 0;
	var AlteHoehe = 0;
	var AlteURL = self.location.href;
	var SplitURL = AlteURL.split("?");
	if (SplitURL.length > 1) {
		var Parameter = SplitURL[1].split("&");
		for (x = 0; x < Parameter.length ; x++ ) {
			if (Parameter[x].indexOf("breite=") != -1) { AlteBreite = Parameter[x].substring(7); }
			else if (Parameter[x].indexOf("hoehe=") != -1) { AlteHoehe = Parameter[x].substring(6); }
		}
	}
	if ((AlteBreite != Breite) || (AlteHoehe != Hoehe)) {
		var NeueParameter = "";
		var GefundenBreite = 0;
		var GefundenHoehe = 0;
		var AlteURL = self.location.href;
		var SplitURL = AlteURL.split("?");
		if (SplitURL.length > 1) {
			var Parameter = SplitURL[1].split("&");
			for (x = 0; x < Parameter.length ; x++ ) {
				if (Parameter[x].indexOf("breite=") != -1) {
					Parameter[x] = "breite="+ Breite;
					GefundenBreite = 1;
				} else if (Parameter[x].indexOf("hoehe=") != -1) {
					Parameter[x] = "hoehe="+ Hoehe;
					GefundenHoehe = 1;
				}
				if (x > 0) { NeueParameter = NeueParameter +"&"; }
				NeueParameter = NeueParameter + Parameter[x];
			}
			if (GefundenBreite == 0) { NeueParameter = NeueParameter + "&breite="+ Breite; }
			if (GefundenHoehe == 0) { NeueParameter = NeueParameter + "&hoehe="+ Hoehe;	}
		} else { NeueParameter = "breite="+ Breite +"&hoehe="+ Hoehe; }
		var NeueURL = SplitURL[0] +"?"+ NeueParameter;
		self.location.href = NeueURL;
	}
}

function an(NavIcon, NavStyle, NavTyp, NavName, NavInfo, NavX, NavY, NavZ) {

	BoxText =	"<table cellspacing=0 cellpadding=0 border=0><tr>" +
		        "<td valign=top><img src=bilder/icon"+ NavIcon +".gif width=36 height=36 border=0></td>" +
				"<td id=header><span id="+ NavStyle +">"+ NavTyp +"</span><br>"+ NavName +"</td>"+
				"</tr></table>"+ NavInfo +"<hr size=1 noshade>" +
				"<table cellspacing=0 cellpadding=0 border=0>" +
				"<tr><td id=pos>X:&nbsp;</td><td id=pos>"+ NavX +"</td></tr>" +
				"<tr><td id=pos>Y:&nbsp;</td><td id=pos>"+ NavY +"</td></tr>" +
				"<tr><td id=pos>Z:&nbsp;</td><td id=pos>"+ NavZ +"</td></tr>" +
				"</table>";

	if ((ns) || (ie) || (mz)) {
		if (FolgeMaus == 0) {
			BoxWrite(BoxText);
			BoxCalcXY(InfoBox);
			BoxAn(InfoBox);
			FolgeMaus = 1;
		}
	}
//	window.status = Navs[Position+5];
}

function aus() {
	FolgeMaus = 0;
	BoxAus(InfoBox);
//	window.status = '';
}

function BoxAn(Objekt) {
	if (ns) Objekt.visibility = "show";
	if (ie) Objekt.visibility = "visible";
	if (mz) Objekt.style.visibility = "visible";
}

function BoxAus(Objekt) {
	if (ns) Objekt.visibility = "hide";
	if (ie) Objekt.visibility = "hidden";
	if (mz) Objekt.style.visibility = "hidden";
}

function BoxWrite(Text) {
	Text += "\n";
	if (ns) {
		var lyr = StarMap.document.mouseoverbox.document;
		lyr.write(Text);
		lyr.close();
	} else if (ie) {
		StarMap.document.all["mouseoverbox"].innerHTML = Text
	} else if (mz) {
		range = StarMap.document.createRange();
		range.setStartBefore(InfoBox);
		domfrag = range.createContextualFragment(Text);
		while (InfoBox.hasChildNodes()) {
			InfoBox.removeChild(InfoBox.lastChild);
		}
		InfoBox.appendChild(domfrag);
	}
}

function BoxCalcXY(Objekt) {
	var realX, realY;
	if (ns) {
		realeBreite = StarMap.clip.width;
		realeHoehe = StarMap.clip.height;
	}
	if (ie) {
		realeBreite = StarMap.document.all['mouseoverbox'].offsetWidth;
		realeHoehe = StarMap.document.all['mouseoverbox'].offsetHeight;
	}
	if (mz) {
		realeBreite = Objekt.offsetWidth;
		realeHoehe = Objekt.offsetHeight;
	}
	scrollX = (ie) ? eval('StarMap.'+docRoot+'.scrollLeft') : StarMap.pageXOffset;
	if (ie) iwidth = eval('StarMap.'+docRoot+'.clientWidth');
	if (ns || mz) iwidth = StarMap.innerWidth;
	if ((mouseX + offsetX + realeBreite + Rand) > (iwidth)) {
		// Links vom Mauszeiger
		realX = mouseX - offsetX - realeBreite;
		if (ie) realX += scrollX;
		if (realX < scrollX) realX = scrollX + Rand;
	} else {
		// Rechts vom Mauszeiger
		realX = mouseX + offsetX;
		if (ie) realX += scrollX;
	}
	scrollY = (ie) ? eval('StarMap.'+docRoot+'.scrollTop') : StarMap.pageYOffset;
	if (ie) iheight = eval('StarMap.'+docRoot+'.clientHeight');
	if (ns || mz) iheight = StarMap.innerHeight;
	if ((mouseY + offsetY + realeHoehe + Rand) > (iheight)) {
		// Über dem Mauszeiger
		realY = mouseY - offsetY - realeHoehe;
		if (ie) realY += scrollY;
		if (realY < scrollY) realY = scrollY + Rand;
	} else {
		// Unter dem Mauszeiger
		realY = mouseY + offsetY;
		if (ie) realY += scrollY;
	}
	BoxMove(Objekt,realX,realY);
}

function BoxMove(Objekt,oX,oY) {
	if ( (ns) || (ie) ) {
		Objekt.left = (ie ? oX + 'px' : oX);
		Objekt.top = (ie ? oY + 'px' : oY);
	} else if (mz) {
		Objekt.style.left = oX + "px";
		Objekt.style.top = oY + "px";
	}
}

function mouseMove(e) {
	if ( (ns) || (mz) ) {
		mouseX = e.pageX;
		mouseY = e.pageY;
	}
	if (ie) {
		mouseX = event.x;
		mouseY = event.y;
	}
	if (ie5) {
		mouseX = eval('event.x+mouseoverbox.'+docRoot+'.scrollLeft');
		mouseY = eval('event.y+mouseoverbox.'+docRoot+'.scrollTop');
	}
	if (FolgeMaus == 1) BoxCalcXY(InfoBox);
}

function Fake() {
	return true;
}
