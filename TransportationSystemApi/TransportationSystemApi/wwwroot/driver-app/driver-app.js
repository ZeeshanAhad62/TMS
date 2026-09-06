/* Mobile driver app -- a small vanilla-JS client for api/driver-app/*.
   Served static from the API's own origin, so no CORS. Token in localStorage. */
(function () {
  "use strict";
  var API = "/api/driver-app";
  var TOKEN_KEY = "driverAppToken";
  var view = document.getElementById("view");
  var appbar = document.getElementById("appbar");
  var appTitle = document.getElementById("appTitle");
  var backBtn = document.getElementById("backBtn");
  var logoutBtn = document.getElementById("logoutBtn");

  function token() { try { return localStorage.getItem(TOKEN_KEY); } catch (e) { return null; } }
  function setToken(t) { try { t ? localStorage.setItem(TOKEN_KEY, t) : localStorage.removeItem(TOKEN_KEY); } catch (e) {} }

  function toast(msg) {
    var el = document.getElementById("toast");
    el.textContent = msg; el.hidden = false;
    clearTimeout(toast._t); toast._t = setTimeout(function () { el.hidden = true; }, 2200);
  }

  async function api(path, opts) {
    opts = opts || {};
    opts.headers = opts.headers || {};
    var t = token();
    if (t) opts.headers["Authorization"] = "Bearer " + t;
    if (opts.json !== undefined) {
      opts.headers["Content-Type"] = "application/json";
      opts.body = JSON.stringify(opts.json);
      delete opts.json;
    }
    var res = await fetch(API + path, opts);
    if (res.status === 401) { setToken(null); location.hash = "#/login"; throw new Error("Session expired -- sign in again."); }
    if (!res.ok) {
      var text = await res.text();
      throw new Error(text || ("Request failed (" + res.status + ")"));
    }
    if (res.status === 204) return null;
    var ct = res.headers.get("content-type") || "";
    return ct.indexOf("application/json") >= 0 ? res.json() : res.text();
  }

  function esc(s) { return String(s == null ? "" : s).replace(/[&<>"]/g, function (c) { return ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" })[c]; }); }
  function badge(s) { return '<span class="badge b-' + esc(s) + '">' + esc(s) + "</span>"; }

  function chrome(opts) {
    appbar.hidden = false;
    appTitle.textContent = opts.title || "Driver App";
    backBtn.hidden = !opts.back;
    backBtn.onclick = function () { location.hash = opts.back || "#/trips"; };
    logoutBtn.hidden = !opts.logout;
  }

  logoutBtn.onclick = function () { setToken(null); location.hash = "#/login"; };

  // ----- Screens -----

  function loginScreen() {
    chrome({ title: "Sign in" });
    view.innerHTML =
      '<div class="card"><h1>Driver sign in</h1>' +
      '<div id="e"></div>' +
      '<label>Driver code</label><input id="code" autocapitalize="characters" autocomplete="username" placeholder="DRV-00001" />' +
      '<label>Password</label><input id="pw" type="password" autocomplete="current-password" />' +
      '<button class="primary" id="go">Sign in</button>' +
      '<p class="muted" style="margin-top:.9rem">Ask your fleet office to enable app access and set your password.</p></div>';
    document.getElementById("go").onclick = async function () {
      var btn = this; btn.disabled = true;
      document.getElementById("e").innerHTML = "";
      try {
        var r = await api("/login", { method: "POST", json: { driverCode: document.getElementById("code").value.trim(), password: document.getElementById("pw").value } });
        setToken(r.token);
        location.hash = "#/trips";
      } catch (err) {
        document.getElementById("e").innerHTML = '<div class="err">' + esc(err.message) + "</div>";
        btn.disabled = false;
      }
    };
  }

  async function tripsScreen() {
    if (!token()) { location.hash = "#/login"; return; }
    chrome({ title: "My trips", logout: true });
    view.innerHTML = '<p class="muted">Loading...</p>';
    try {
      var me = await api("/me");
      var trips = await api("/trips");
      var html = '<div class="card"><div class="meta">Signed in as</div><div style="font-weight:600">' + esc(me.fullName) + " &middot; " + esc(me.driverCode) + "</div></div>";
      if (!trips.length) html += '<div class="card muted">No trips assigned to you.</div>';
      trips.forEach(function (t) {
        html += '<a class="card trip" href="#/trip?id=' + t.id + '">' +
          '<div class="code">' + esc(t.tripCode) + " &middot; " + esc(t.vehicleCode) + "</div>" +
          '<div class="route">' + esc(t.origin) + " &rarr; " + esc(t.destination) + "</div>" +
          '<div class="meta">' + esc(t.startDate) + " &nbsp; " + badge(t.status) +
          (t.consignmentCount ? ' &nbsp; <span class="pill">' + t.consignmentCount + " LR" + (t.pendingPodCount ? " &middot; " + t.pendingPodCount + " POD due" : "") + "</span>" : "") +
          "</div></a>";
      });
      view.innerHTML = html;
    } catch (err) { view.innerHTML = '<div class="err">' + esc(err.message) + "</div>"; }
  }

  async function tripScreen(id) {
    if (!token()) { location.hash = "#/login"; return; }
    chrome({ title: "Trip", back: "#/trips", logout: true });
    view.innerHTML = '<p class="muted">Loading...</p>';
    var t;
    try { t = await api("/trips/" + id); }
    catch (err) { view.innerHTML = '<div class="err">' + esc(err.message) + "</div>"; return; }

    var html = '<div class="card">' +
      '<div class="code">' + esc(t.tripCode) + " &middot; " + esc(t.vehicleCode) + " (" + esc(t.vehicleRegistrationNumber) + ")</div>" +
      '<div class="route">' + esc(t.origin) + " &rarr; " + esc(t.destination) + "</div>" +
      '<div class="meta">' + esc(t.startDate) + (t.endDate ? " &ndash; " + esc(t.endDate) : "") + " &nbsp; " + badge(t.status) + "</div>" +
      (t.customerName ? '<div class="meta">Customer: ' + esc(t.customerName) + "</div>" : "") +
      '<div class="row" style="margin-top:.8rem">' +
        (t.status === "Scheduled" ? '<button class="act go" data-status="Active">Start trip</button>' : "") +
        (t.status === "Active" ? '<button class="act done" data-status="Completed">Finish trip</button>' : "") +
      "</div></div>";

    // Consignments
    html += '<div class="card"><h2>Consignments (' + t.consignments.length + ")</h2>";
    if (!t.consignments.length) html += '<div class="muted">None on this trip.</div>';
    t.consignments.forEach(function (c) {
      html += '<div class="list-row"><div class="grow">' +
        '<div class="t">' + esc(c.consignorName) + " &rarr; " + esc(c.consigneeName) + "</div>" +
        '<div class="meta">' + esc(c.consignmentCode) + (c.lrNumber ? " &middot; LR " + esc(c.lrNumber) : "") + " " + badge(c.status) + (c.hasPod ? " &middot; POD ✓" : "") + "</div>" +
        "</div>" +
        (c.status !== "Delivered" && c.status !== "Cancelled" ? '<button class="act" data-deliver="' + c.id + '">Deliver</button>' : "") +
        '<button class="act" data-pod="' + c.id + '">POD</button>' +
        "</div>";
    });
    html += "</div>";

    // Expenses
    html += '<div class="card"><h2>Log expense</h2>' +
      '<label>Category</label><select id="expCat">' +
        ["Toll", "Parking", "LoadingUnloading", "DriverAllowance", "EnRouteRepair", "Fine", "Weighbridge", "Misc"].map(function (x) { return '<option value="' + x + '">' + x + "</option>"; }).join("") +
      "</select>" +
      '<div class="row"><div><label>Amount</label><input id="expAmt" type="number" inputmode="decimal" /></div>' +
      '<div><label>Note</label><input id="expNote" /></div></div>' +
      '<button class="primary" id="expGo">Add expense</button>';
    if (t.expenses.length) {
      html += "<hr /><div class='meta'>Recent: total " + t.expensesTotal.toFixed(2) + "</div>";
      t.expenses.slice(0, 5).forEach(function (e) {
        html += '<div class="list-row"><div class="grow"><div class="t">' + esc(e.category) + " &mdash; " + e.amount.toFixed(2) + "</div><div class='meta'>" + esc(e.date) + (e.notes ? " &middot; " + esc(e.notes) : "") + "</div></div></div>";
      });
    }
    html += "</div>";

    // Fuel
    html += '<div class="card"><h2>Log fuel</h2>' +
      '<div class="row"><div><label>Odometer</label><input id="fOdo" type="number" inputmode="decimal" /></div>' +
      '<div><label>Litres</label><input id="fLit" type="number" inputmode="decimal" /></div></div>' +
      '<div class="row"><div><label>Rate / litre</label><input id="fRate" type="number" inputmode="decimal" /></div>' +
      '<div><label>Station</label><input id="fSta" /></div></div>' +
      '<label><input type="checkbox" id="fFull" checked style="width:auto;margin-right:.4rem" />Tank filled full</label>' +
      '<button class="primary" id="fGo">Add fuel entry</button></div>';

    view.innerHTML = html;

    view.querySelectorAll("button[data-status]").forEach(function (b) {
      b.onclick = async function () {
        try { await api("/trips/" + id + "/status", { method: "POST", json: { status: b.getAttribute("data-status") } }); toast("Trip " + b.getAttribute("data-status")); tripScreen(id); }
        catch (err) { toast(err.message); }
      };
    });
    view.querySelectorAll("button[data-deliver]").forEach(function (b) {
      b.onclick = async function () {
        var who = prompt("Received by (name):");
        if (!who) return;
        try { await api("/consignments/" + b.getAttribute("data-deliver") + "/deliver", { method: "POST", json: { receivedBy: who } }); toast("Marked delivered"); tripScreen(id); }
        catch (err) { toast(err.message); }
      };
    });
    view.querySelectorAll("button[data-pod]").forEach(function (b) {
      b.onclick = function () {
        var inp = document.createElement("input");
        inp.type = "file"; inp.accept = ".pdf,.jpg,.jpeg,.png,image/*";
        inp.onchange = async function () {
          if (!inp.files[0]) return;
          var fd = new FormData(); fd.append("file", inp.files[0]);
          try { await api("/consignments/" + b.getAttribute("data-pod") + "/pod", { method: "POST", body: fd }); toast("POD uploaded"); tripScreen(id); }
          catch (err) { toast(err.message); }
        };
        inp.click();
      };
    });
    document.getElementById("expGo").onclick = async function () {
      var amt = parseFloat(document.getElementById("expAmt").value);
      if (!(amt > 0)) { toast("Enter an amount"); return; }
      try {
        await api("/trips/" + id + "/expenses", { method: "POST", json: { category: document.getElementById("expCat").value, amount: amt, notes: document.getElementById("expNote").value || null } });
        toast("Expense added"); tripScreen(id);
      } catch (err) { toast(err.message); }
    };
    document.getElementById("fGo").onclick = async function () {
      var lit = parseFloat(document.getElementById("fLit").value), rate = parseFloat(document.getElementById("fRate").value);
      if (!(lit > 0) || !(rate >= 0)) { toast("Enter litres and rate"); return; }
      try {
        await api("/trips/" + id + "/fuel", { method: "POST", json: {
          odometerReading: parseFloat(document.getElementById("fOdo").value) || 0,
          litres: lit, ratePerLitre: rate,
          isTankFull: document.getElementById("fFull").checked,
          stationName: document.getElementById("fSta").value || null
        }});
        toast("Fuel entry added"); tripScreen(id);
      } catch (err) { toast(err.message); }
    };
  }

  // ----- Router -----

  function route() {
    var h = location.hash || (token() ? "#/trips" : "#/login");
    if (h.indexOf("#/trip?") === 0) { tripScreen(new URLSearchParams(h.split("?")[1]).get("id")); return; }
    if (h === "#/trips") { tripsScreen(); return; }
    loginScreen();
  }
  window.addEventListener("hashchange", route);
  route();
})();
