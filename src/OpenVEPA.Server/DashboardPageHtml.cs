namespace OpenVEPA.Server;

/// <summary>Contains the embedded HTML for the post-setup dashboard page served at <c>GET /</c>.</summary>
internal static class DashboardPageHtml
{
    /// <summary>The complete HTML page for the dashboard with status, connection info, and quick actions.</summary>
    internal const string Content = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>OpenVEPA Dashboard</title>
<style>
*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
body{
    font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Oxygen,Ubuntu,sans-serif;
    background:#1a1a2e;color:#e0e0e0;min-height:100vh;
    display:flex;justify-content:center;align-items:center;padding:2rem;
}
.card{
    background:#16213e;border-radius:12px;padding:2.5rem;
    width:100%;max-width:720px;box-shadow:0 8px 32px rgba(0,0,0,.4);
}
.brand{text-align:center;margin-bottom:1.5rem}
.brand h1{color:#50fa7b;font-size:2rem;letter-spacing:.04em}
.brand p{color:#888;font-size:.95rem;margin-top:.25rem}

/* Status bar */
.status-bar{
    display:flex;align-items:center;justify-content:space-between;
    background:#0f3460;border-radius:8px;padding:.75rem 1.25rem;margin-bottom:1.75rem;
}
.status-left{display:flex;align-items:center;gap:.6rem}
.pulse{
    width:10px;height:10px;border-radius:50%;background:#50fa7b;
    animation:pulse-anim 1.5s ease-in-out infinite;
}
@keyframes pulse-anim{
    0%,100%{box-shadow:0 0 0 0 rgba(80,250,123,.6)}
    50%{box-shadow:0 0 0 6px rgba(80,250,123,0)}
}
.status-left span{font-weight:600;color:#50fa7b}
.version{color:#888;font-size:.85rem}

/* Section headers */
.section-title{
    font-size:.8rem;text-transform:uppercase;letter-spacing:.08em;
    color:#888;margin-bottom:.75rem;margin-top:1.5rem;
}

/* Info grid */
.info-grid{display:grid;grid-template-columns:120px 1fr;gap:.5rem .75rem;align-items:center}
.info-grid .label{color:#888;font-size:.9rem}
.info-grid .value{color:#e0e0e0;font-size:.9rem}

/* Connection rows */
.conn-row{
    display:flex;align-items:center;justify-content:space-between;
    background:#0f3460;border-radius:6px;padding:.6rem 1rem;margin-bottom:.5rem;
}
.conn-row .conn-label{color:#888;font-size:.8rem;min-width:110px}
.conn-row .conn-value{
    color:#e0e0e0;font-size:.85rem;font-family:'SFMono-Regular',Consolas,'Liberation Mono',monospace;
    word-break:break-all;flex:1;margin:0 .75rem;
}
.copy-btn{
    background:transparent;border:1px solid #2a2a4e;border-radius:4px;
    color:#888;cursor:pointer;padding:.25rem .5rem;font-size:.75rem;
    transition:border-color .2s,color .2s;white-space:nowrap;
}
.copy-btn:hover{border-color:#50fa7b;color:#50fa7b}
.copy-btn.copied{border-color:#50fa7b;color:#50fa7b}

/* Buttons */
.actions{display:flex;gap:.75rem;margin-top:1.75rem;flex-wrap:wrap}
.btn{
    flex:1;min-width:140px;padding:.7rem 1rem;border-radius:6px;
    font-size:.9rem;font-weight:600;cursor:pointer;text-align:center;
    text-decoration:none;transition:background .2s;border:none;
}
.btn-primary{background:#50fa7b;color:#1a1a2e}
.btn-primary:hover{background:#40d66b}
.btn-secondary{background:#0f3460;color:#e0e0e0;border:1px solid #2a2a4e}
.btn-secondary:hover{background:#162d54}
.btn-disabled{background:#0f3460;color:#555;border:1px solid #2a2a4e;cursor:not-allowed}

/* Footer */
.footer{
    text-align:center;margin-top:2rem;padding-top:1.25rem;
    border-top:1px solid #2a2a4e;
}
.footer p{color:#555;font-size:.8rem}
.footer a{color:#888;text-decoration:none;transition:color .2s}
.footer a:hover{color:#50fa7b}

@media(max-width:600px){
    body{padding:1rem}
    .card{padding:1.5rem}
    .actions{flex-direction:column}
    .btn{min-width:unset}
    .conn-row{flex-direction:column;align-items:flex-start;gap:.4rem}
    .conn-row .conn-value{margin:0}
}
</style>
</head>
<body>
<div class="card">
    <div class="brand">
        <h1>OpenVEPA</h1>
        <p>Personal AI Assistant</p>
    </div>

    <div class="status-bar">
        <div class="status-left">
            <div class="pulse"></div>
            <span>Server Running</span>
        </div>
        <div class="version">v1.0.0</div>
    </div>

    <div class="section-title">Quick Info</div>
    <div class="info-grid">
        <div class="label">Provider</div>
        <div class="value"><span id="provider">—</span></div>
        <div class="label">Model</div>
        <div class="value"><span id="model">—</span></div>
        <div class="label">Uptime</div>
        <div class="value"><span id="uptime">—</span></div>
    </div>

    <div class="section-title">Connection Info</div>
    <div class="conn-row">
        <span class="conn-label">WebSocket Hub</span>
        <span class="conn-value" id="ws-url">ws://localhost/hub/assistant</span>
        <button class="copy-btn" onclick="copyText('ws-url')">Copy</button>
    </div>
    <div class="conn-row">
        <span class="conn-label">Health Check</span>
        <span class="conn-value" id="health-url">http://localhost/health</span>
        <button class="copy-btn" onclick="copyText('health-url')">Copy</button>
    </div>
    <div class="conn-row">
        <span class="conn-label">API Docs</span>
        <span class="conn-value">Coming soon</span>
    </div>

    <div class="section-title">Quick Actions</div>
    <div class="actions">
        <a href="/init/setupwizard" class="btn btn-primary">Reconfigure</a>
        <a href="/health" target="_blank" rel="noopener" class="btn btn-secondary">Health Check</a>
        <span class="btn btn-disabled" title="Coming soon">API Tokens</span>
    </div>

    <div class="footer">
        <p>OpenVEPA — Open Virtual Executive Personal Assistant</p>
        <p style="margin-top:.4rem"><a href="#">GitHub</a></p>
    </div>
</div>
<script>
(function(){
    var h=window.location.host;
    var proto=window.location.protocol;
    var wsProto=proto==='https:'?'wss:':'ws:';
    document.getElementById('ws-url').textContent=wsProto+'//'+h+'/hub/assistant';
    document.getElementById('health-url').textContent=proto+'//'+h+'/health';

    fetch('/api/status').then(function(r){return r.json()}).then(function(data){
        document.getElementById('provider').textContent=data.provider||'\u2014';
        document.getElementById('model').textContent=data.model||'\u2014';
        document.getElementById('uptime').textContent=data.uptime||'\u2014';
    }).catch(function(){});
})();

function copyText(id){
    var el=document.getElementById(id);
    if(!el)return;
    var text=el.textContent;
    if(navigator.clipboard&&navigator.clipboard.writeText){
        navigator.clipboard.writeText(text).then(function(){flashCopied(el)}).catch(function(){});
    }else{
        var ta=document.createElement('textarea');
        ta.value=text;ta.style.position='fixed';ta.style.opacity='0';
        document.body.appendChild(ta);ta.select();
        try{document.execCommand('copy');flashCopied(el)}catch(e){}
        document.body.removeChild(ta);
    }
}
function flashCopied(el){
    var btn=el.parentElement.querySelector('.copy-btn');
    if(!btn)return;
    btn.textContent='Copied!';btn.classList.add('copied');
    setTimeout(function(){btn.textContent='Copy';btn.classList.remove('copied')},1500);
}
</script>
</body>
</html>
""";
}
