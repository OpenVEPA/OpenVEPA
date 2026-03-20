namespace OpenVEPA.Server;

/// <summary>Contains the embedded HTML for the main OpenVEPA WebUI shell served at <c>GET /</c>.</summary>
internal static class DashboardPageHtml
{
    /// <summary>The complete HTML page for the Single Page Application shell with sidebar navigation.</summary>
    internal const string Content = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>OpenVEPA WebUI</title>
<style>
:root{
    --bg-main:#1a1a2e;
    --bg-panel:#16213e;
    --bg-surface:#0f3460;
    --bg-hover:#1d2a4a;
    --border:#2a2a4e;
    --accent:#50fa7b;
    --text-primary:#e0e0e0;
    --text-secondary:#888;
    --shadow:0 18px 42px rgba(0,0,0,.28);
    --sidebar-width:280px;
    --sidebar-collapsed-width:88px;
}
[data-theme="dark"]{
    --bg-main:#1a1a2e;
    --bg-panel:#16213e;
    --bg-surface:#0f3460;
    --bg-hover:#1d2a4a;
    --border:#2a2a4e;
    --accent:#50fa7b;
    --text-primary:#e0e0e0;
    --text-secondary:#888;
    --shadow:0 18px 42px rgba(0,0,0,.28);
}
[data-theme="light"]{
    --bg-main:#f0f2f5;
    --bg-panel:#ffffff;
    --bg-surface:#e8edf2;
    --bg-hover:#dce3eb;
    --border:#d0d7de;
    --accent:#2da44e;
    --text-primary:#1f2328;
    --text-secondary:#656d76;
    --shadow:0 8px 24px rgba(0,0,0,.08);
}
*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
html,body{height:100%}
body{
    font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Oxygen,Ubuntu,sans-serif;
    background:var(--bg-main);
    color:var(--text-primary);
    overflow:hidden;
}
a{color:inherit;text-decoration:none}
button,input,select,textarea{font:inherit}
button{cursor:pointer}
button:focus-visible,a:focus-visible,input:focus-visible,select:focus-visible,textarea:focus-visible{
    outline:2px solid var(--accent);
    outline-offset:2px;
}

/* App shell layout */
.app-shell{
    min-height:100vh;
    display:grid;
    grid-template-columns:var(--sidebar-width) 1fr;
    transition:grid-template-columns .25s ease;
}
body.sidebar-collapsed .app-shell{grid-template-columns:var(--sidebar-collapsed-width) 1fr}

/* Sidebar */
.sidebar{
    background:var(--bg-panel);
    border-right:1px solid var(--border);
    display:flex;
    flex-direction:column;
    height:100vh;
    overflow:hidden;
    position:relative;
    z-index:30;
    transition:transform .25s ease,width .25s ease;
}
.sidebar > nav{
    flex:1;
    overflow-y:auto;
    overflow-x:hidden;
}
.sidebar-header{
    padding:1.4rem 1.25rem 1rem;
    border-bottom:1px solid var(--border);
}
.sidebar-brand{
    display:flex;
    align-items:center;
    gap:.9rem;
}
.sidebar-mark{
    width:2.4rem;
    height:2.4rem;
    border-radius:.8rem;
    display:grid;
    place-items:center;
    background:rgba(80,250,123,.14);
    color:var(--accent);
    font-weight:800;
    letter-spacing:.08em;
}
.sidebar-brand-text{min-width:0}
.sidebar-brand-title{
    color:var(--accent);
    font-weight:800;
    letter-spacing:.08em;
}
.sidebar-brand-subtitle{
    color:var(--text-secondary);
    font-size:.82rem;
    margin-top:.18rem;
}
.sidebar-caption{
    color:var(--text-secondary);
    font-size:.78rem;
    letter-spacing:.08em;
    text-transform:uppercase;
    padding:1rem 1.25rem .55rem;
}
.nav-list{
    list-style:none;
    padding:.25rem 0;
    display:flex;
    flex-direction:column;
    gap:.2rem;
}
.nav-link{
    display:flex;
    align-items:center;
    gap:.9rem;
    padding:.95rem 1.25rem;
    border-left:4px solid transparent;
    color:var(--text-primary);
    transition:background .2s ease,border-color .2s ease,color .2s ease;
}
.nav-link:hover{background:var(--bg-hover)}
.nav-link.is-active{
    background:rgba(80,250,123,.08);
    border-left-color:var(--accent);
}
.nav-icon{
    width:1.75rem;
    text-align:center;
    flex:0 0 1.75rem;
    font-size:1.15rem;
}
.nav-copy{display:block;min-width:0}
.nav-title{display:block;font-weight:600}
.nav-description{
    display:block;
    color:var(--text-secondary);
    font-size:.78rem;
    margin-top:.12rem;
}
.sidebar-footer{
    margin-top:auto;
    border-top:1px solid var(--border);
    padding:1rem 1.25rem 1.35rem;
    color:var(--text-secondary);
    font-size:.82rem;
    flex-shrink:0;
}
.theme-picker{
    display:flex;
    gap:.35rem;
    margin-top:.7rem;
}
.theme-picker-label{
    font-size:.72rem;
    text-transform:uppercase;
    letter-spacing:.06em;
    color:var(--text-secondary);
    margin-bottom:.25rem;
}
.theme-btn{
    flex:1;
    padding:.35rem 0;
    border:1px solid var(--border);
    border-radius:.45rem;
    background:transparent;
    color:var(--text-secondary);
    font-size:.78rem;
    text-align:center;
    transition:background .2s,color .2s,border-color .2s;
    cursor:pointer;
}
.theme-btn:hover{background:var(--bg-hover);color:var(--text-primary)}
.theme-btn.is-active{
    background:var(--bg-surface);
    color:var(--accent);
    border-color:var(--accent);
}
body.sidebar-collapsed .sidebar-brand-text,
body.sidebar-collapsed .sidebar-caption,
body.sidebar-collapsed .nav-copy,
body.sidebar-collapsed .sidebar-footer{
    opacity:0;
    width:0;
    height:0;
    overflow:hidden;
    pointer-events:none;
}
body.sidebar-collapsed .sidebar-header,
body.sidebar-collapsed .nav-link{padding-left:1rem;padding-right:1rem}
body.sidebar-collapsed .sidebar-brand,
body.sidebar-collapsed .nav-link{justify-content:center}
body.sidebar-collapsed .nav-link{border-left-color:transparent}
body.sidebar-collapsed .nav-link.is-active{
    background:rgba(80,250,123,.08);
    box-shadow:inset 0 0 0 1px rgba(80,250,123,.2);
}

/* Main area */
.main-area{
    min-width:0;
    height:100vh;
    display:grid;
    grid-template-rows:auto 1fr;
}
.topbar{
    display:flex;
    align-items:center;
    justify-content:space-between;
    gap:1rem;
    padding:1rem 1.5rem;
    background:rgba(22,33,62,.92);
    border-bottom:1px solid var(--border);
    backdrop-filter:blur(10px);
}
.topbar-left{display:flex;align-items:center;gap:1rem;min-width:0}
.menu-toggle{
    width:2.9rem;
    height:2.9rem;
    border-radius:.9rem;
    border:1px solid var(--border);
    background:var(--bg-surface);
    color:var(--text-primary);
    display:grid;
    place-items:center;
    transition:background .2s ease,border-color .2s ease,transform .2s ease;
}
.menu-toggle:hover{background:var(--bg-hover);border-color:rgba(80,250,123,.35)}
.menu-toggle:active{transform:scale(.98)}
.menu-bars{font-size:1.1rem;line-height:1}
.topbar-brand{min-width:0}
.topbar-brand-row{display:flex;align-items:center;gap:.7rem;flex-wrap:wrap}
.topbar-title{
    color:var(--accent);
    font-size:1.3rem;
    font-weight:800;
    letter-spacing:.06em;
}
.topbar-page{
    color:var(--text-primary);
    font-size:1rem;
    font-weight:600;
}
.topbar-subtitle{
    color:var(--text-secondary);
    font-size:.88rem;
    margin-top:.22rem;
    white-space:nowrap;
    overflow:hidden;
    text-overflow:ellipsis;
}
.topbar-status{
    display:flex;
    align-items:center;
    gap:.55rem;
    color:var(--text-secondary);
    font-size:.88rem;
    white-space:nowrap;
}
.status-dot{
    width:.65rem;
    height:.65rem;
    border-radius:999px;
    background:var(--accent);
    box-shadow:0 0 0 6px rgba(80,250,123,.12);
}
.page-content{
    overflow:auto;
    padding:1.5rem;
}
.page-shell{
    max-width:1200px;
    margin:0 auto;
    display:flex;
    flex-direction:column;
    gap:1rem;
    min-height:0;
}
.page-header{
    display:flex;
    align-items:flex-start;
    justify-content:space-between;
    gap:1rem;
    flex-wrap:wrap;
}
.page-header h1{
    font-size:1.75rem;
    margin-bottom:.35rem;
}
.page-header p{
    color:var(--text-secondary);
    max-width:62rem;
    line-height:1.55;
}
.card{
    background:var(--bg-panel);
    border:1px solid var(--border);
    border-radius:18px;
    box-shadow:var(--shadow);
}
.section-heading{
    display:flex;
    justify-content:space-between;
    align-items:flex-start;
    gap:1rem;
    flex-wrap:wrap;
    margin-bottom:1rem;
}
.section-heading h2{font-size:1.08rem;margin-bottom:.25rem}
.section-heading p,.helper-text{color:var(--text-secondary);line-height:1.5}
.pill{
    display:inline-flex;
    align-items:center;
    gap:.4rem;
    border-radius:999px;
    padding:.38rem .8rem;
    font-size:.8rem;
    border:1px solid rgba(80,250,123,.22);
    background:rgba(80,250,123,.08);
    color:var(--accent);
    white-space:nowrap;
}
.placeholder-card{
    padding:1.6rem;
    display:flex;
    flex-direction:column;
    gap:1rem;
}
.placeholder-icon{
    width:3.2rem;
    height:3.2rem;
    border-radius:1rem;
    display:grid;
    place-items:center;
    background:rgba(80,250,123,.08);
    font-size:1.55rem;
}
.placeholder-copy h2{margin-bottom:.45rem}
.placeholder-copy p{color:var(--text-secondary);line-height:1.6;max-width:58rem}
.placeholder-meta{
    display:grid;
    gap:.85rem;
    grid-template-columns:repeat(auto-fit,minmax(220px,1fr));
}
.meta-box{
    background:rgba(15,52,96,.55);
    border:1px solid var(--border);
    border-radius:14px;
    padding:1rem;
}
.meta-box h3{font-size:.95rem;margin-bottom:.35rem}
.meta-box p{color:var(--text-secondary);font-size:.92rem;line-height:1.5}
.loading-indicator{display:flex;align-items:center;gap:.35rem}
.loading-indicator span{
    width:.45rem;
    height:.45rem;
    border-radius:999px;
    background:var(--accent);
    opacity:.35;
    animation:loading-pulse 1.1s ease-in-out infinite;
}
.loading-indicator span:nth-child(2){animation-delay:.15s}
.loading-indicator span:nth-child(3){animation-delay:.3s}
@keyframes loading-pulse{
    0%,100%{transform:translateY(0);opacity:.35}
    50%{transform:translateY(-3px);opacity:1}
}

/* Home / chat page */
.home-layout{
    flex:1;
    min-height:0;
    display:grid;
    grid-template-rows:1fr auto;
    gap:1rem;
}
.chat-panel{
    padding:1.25rem;
    min-height:0;
    display:grid;
    grid-template-rows:auto 1fr;
}
.chat-messages{
    min-height:320px;
    overflow:auto;
    display:flex;
    flex-direction:column;
    gap:1rem;
    padding-right:.25rem;
}
.message-row{
    display:flex;
    width:100%;
}
.message-row.is-user{justify-content:flex-end}
.message-row.is-assistant{justify-content:flex-start}
.message-row.is-system{justify-content:center}
.message{
    width:min(100%,52rem);
    padding:1rem 1.1rem;
    border-radius:18px;
    border:1px solid var(--border);
    background:rgba(15,52,96,.55);
    box-shadow:0 10px 22px rgba(0,0,0,.18);
}
.message.assistant,.message.tool{background:rgba(15,52,96,.72)}
.message.user{background:rgba(80,250,123,.12);border-color:rgba(80,250,123,.34)}
.message.system{
    width:min(100%,36rem);
    text-align:center;
    background:rgba(255,255,255,.03);
    border-style:dashed;
    box-shadow:none;
}
.message-header{
    display:flex;
    justify-content:space-between;
    gap:1rem;
    align-items:center;
    margin-bottom:.55rem;
    font-size:.82rem;
}
.message-label{
    color:var(--accent);
    font-weight:700;
    text-transform:uppercase;
    letter-spacing:.08em;
}
.message-timestamp{
    color:var(--text-secondary);
    white-space:nowrap;
}
.message-body{
    line-height:1.65;
    word-break:break-word;
}
.message-body p + p{margin-top:.75rem}
.message-meta{
    margin-top:.75rem;
    display:flex;
    justify-content:space-between;
    gap:.75rem;
    flex-wrap:wrap;
}
.message-usage{
    color:var(--text-secondary);
    font-size:.8rem;
}
.message-empty{
    min-height:100%;
    border:1px dashed rgba(80,250,123,.2);
    border-radius:18px;
    padding:1.6rem;
    display:grid;
    place-items:center;
    text-align:center;
    background:rgba(15,52,96,.3);
}
.message-empty-copy{
    max-width:34rem;
    display:flex;
    flex-direction:column;
    gap:.55rem;
}
.message-empty-copy h3{font-size:1.05rem}
.message-empty-copy p{color:var(--text-secondary);line-height:1.6}
.error-banner{
    border-radius:14px;
    border:1px solid rgba(255,120,120,.28);
    background:rgba(255,120,120,.09);
    color:#ffb3b3;
    padding:.85rem 1rem;
}
.auth-panel,
.chat-composer-panel{
    display:flex;
    flex-direction:column;
    gap:1rem;
}
.auth-panel[hidden],
.chat-composer-panel[hidden]{
    display:none;
}
.auth-panel{
    padding:1.15rem;
    border:1px dashed rgba(80,250,123,.2);
    border-radius:16px;
    background:rgba(15,52,96,.34);
}
.auth-form{
    display:flex;
    flex-direction:column;
    gap:1rem;
}
.chat-composer{
    padding:1.25rem;
    display:flex;
    flex-direction:column;
    gap:1rem;
}
.chat-toolbar,.composer-row{
    display:flex;
    gap:.85rem;
    align-items:flex-end;
    flex-wrap:wrap;
}
.toolbar-actions{
    display:flex;
    gap:.75rem;
    flex-wrap:wrap;
    align-items:center;
}
.field{
    display:flex;
    flex-direction:column;
    gap:.45rem;
    min-width:0;
    flex:1 1 220px;
}
.field span{
    color:var(--text-secondary);
    font-size:.84rem;
}
.control{
    width:100%;
    background:var(--bg-surface);
    border:1px solid var(--border);
    border-radius:12px;
    color:var(--text-primary);
    padding:.85rem 1rem;
    min-height:3rem;
}
.control:disabled{opacity:.85;cursor:not-allowed}
.chat-textarea{
    min-height:7rem;
    resize:vertical;
}
.primary-button,.secondary-button,.subtle-button{
    border-radius:12px;
    border:1px solid transparent;
    min-height:3rem;
    padding:0 1rem;
    transition:background .2s ease,border-color .2s ease,color .2s ease,transform .2s ease;
}
.primary-button{
    background:var(--accent);
    color:var(--bg-main);
    font-weight:800;
}
.primary-button:hover:not(:disabled){transform:translateY(-1px)}
.primary-button:disabled{opacity:.55;cursor:not-allowed}
.secondary-button,.subtle-button{
    background:var(--bg-surface);
    border-color:var(--border);
    color:var(--text-primary);
}
.secondary-button:hover,.subtle-button:hover{background:var(--bg-hover)}
.subtle-button{
    min-height:auto;
    padding:.55rem .85rem;
    font-size:.82rem;
}
.send-button{
    min-width:3.6rem;
    font-size:1.2rem;
    padding:0;
}
.session-select{
    min-width:18rem;
}
.helper-row{
    display:flex;
    justify-content:space-between;
    gap:.75rem;
    flex-wrap:wrap;
    align-items:center;
}
.helper-note{
    color:var(--text-secondary);
    font-size:.84rem;
    line-height:1.5;
}
.text-link{
    color:var(--accent);
    text-decoration:underline;
    text-underline-offset:2px;
}
.auth-cta-stack,
.auth-manual-help,
.token-management-form,
.token-created-box{
    display:flex;
    flex-direction:column;
    gap:1rem;
}
.auth-create-card,
.token-created-box{
    padding:1rem;
    border-radius:14px;
    border:1px solid rgba(80,250,123,.24);
    background:rgba(80,250,123,.08);
}
.auth-create-actions,
.token-created-header,
.token-table-actions{
    display:flex;
    gap:.75rem;
    flex-wrap:wrap;
    align-items:center;
}
.docker-command,
.token-secret{
    margin:0;
}
.empty-action{
    display:inline-flex;
    align-items:center;
    justify-content:center;
    margin-top:.35rem;
}

/* Mobile overlay */
.sidebar-overlay{
    position:fixed;
    inset:0;
    background:rgba(5,7,18,.72);
    opacity:0;
    pointer-events:none;
    transition:opacity .25s ease;
    z-index:20;
}
body.mobile-sidebar-open .sidebar-overlay{opacity:1;pointer-events:auto}

/* Responsive behaviour */
@media(max-width:900px){
    .app-shell{grid-template-columns:1fr}
    .sidebar{
        position:fixed;
        inset:0 auto 0 0;
        width:min(82vw,320px);
        transform:translateX(-100%);
        box-shadow:0 16px 48px rgba(0,0,0,.45);
    }
    body.mobile-sidebar-open .sidebar{transform:translateX(0)}
    body.sidebar-collapsed .sidebar-brand-text,
    body.sidebar-collapsed .sidebar-caption,
    body.sidebar-collapsed .nav-copy,
    body.sidebar-collapsed .sidebar-footer{
        opacity:1;
        width:auto;
        height:auto;
        overflow:visible;
        pointer-events:auto;
    }
    body.sidebar-collapsed .sidebar-header,
    body.sidebar-collapsed .nav-link{padding-left:1.25rem;padding-right:1.25rem}
    body.sidebar-collapsed .sidebar-brand,
    body.sidebar-collapsed .nav-link{justify-content:flex-start}
    .topbar{padding:1rem}
    .page-content{padding:1rem}
    .page-shell{min-height:0}
}
@media(max-width:640px){
    .topbar-brand-row{display:block}
    .topbar-page{display:block;margin-top:.2rem}
    .page-header h1{font-size:1.45rem}
    .chat-toolbar,.composer-row{flex-direction:column;align-items:stretch}
    .field,.primary-button,.secondary-button,.send-button{width:100%}
    .chat-messages{min-height:280px}
}

/* Sessions page */
.sessions-layout{
    display:flex;
    flex-direction:column;
    gap:1rem;
}
.sessions-toolbar-card{
    padding:1.25rem;
    display:flex;
    flex-direction:column;
    gap:1rem;
}
.sessions-toolbar-row{
    display:flex;
    align-items:center;
    gap:1rem;
    flex-wrap:wrap;
}
.sessions-create-toggle{min-width:11rem}
.sessions-search{
    position:relative;
    display:flex;
    align-items:center;
    flex:1 1 320px;
    min-width:0;
}
.sessions-search-icon{
    position:absolute;
    left:1rem;
    color:var(--text-secondary);
    pointer-events:none;
}
.sessions-search-input{padding-left:2.75rem}
.sessions-create-form{
    display:flex;
    align-items:flex-end;
    gap:1rem;
    flex-wrap:wrap;
    padding-top:.25rem;
    border-top:1px solid rgba(255,255,255,.04);
}
.sessions-create-field{flex:1 1 320px}
.sessions-create-actions{
    display:flex;
    gap:.75rem;
    flex-wrap:wrap;
}
.sessions-feedback{min-height:1.35rem}
.sessions-list{
    display:grid;
    gap:1rem;
}
.session-card{
    padding:1.25rem;
    display:flex;
    justify-content:space-between;
    align-items:flex-start;
    gap:1rem;
    flex-wrap:wrap;
}
.session-card-main{
    flex:1 1 420px;
    min-width:0;
}
.session-card-header{
    display:flex;
    align-items:flex-start;
    justify-content:space-between;
    gap:1rem;
    flex-wrap:wrap;
    margin-bottom:.65rem;
}
.session-card-title{
    display:flex;
    align-items:center;
    gap:.75rem;
    min-width:0;
    font-size:1.08rem;
    font-weight:700;
}
.session-card-title span:last-child{
    min-width:0;
    overflow-wrap:anywhere;
}
.session-card-title-icon{
    width:2.5rem;
    height:2.5rem;
    border-radius:.9rem;
    display:grid;
    place-items:center;
    background:rgba(80,250,123,.08);
    flex:0 0 auto;
}
.session-card-meta{
    color:var(--text-secondary);
    line-height:1.6;
}
.session-card-status{
    display:flex;
    align-items:center;
    gap:.55rem;
    flex-wrap:wrap;
    margin-top:.9rem;
}
.session-card-secondary{
    color:var(--text-secondary);
    font-size:.86rem;
    overflow-wrap:anywhere;
}
.session-card-actions{
    display:flex;
    align-items:center;
    gap:.75rem;
    flex-wrap:wrap;
}
.session-action-button{min-width:6.5rem}
.session-status-badge{
    display:inline-flex;
    align-items:center;
    justify-content:center;
    min-width:6rem;
    padding:.45rem .9rem;
    border-radius:999px;
    font-size:.8rem;
    font-weight:700;
    border:1px solid transparent;
}
.session-status-active{
    background:rgba(80,250,123,.12);
    border-color:rgba(80,250,123,.28);
    color:#7bff9d;
}
.session-status-paused{
    background:rgba(255,184,108,.12);
    border-color:rgba(255,184,108,.28);
    color:#ffcf88;
}
.session-status-completed{
    background:rgba(139,180,255,.12);
    border-color:rgba(139,180,255,.28);
    color:#aecdff;
}
.session-status-archived{
    background:rgba(136,136,136,.16);
    border-color:rgba(136,136,136,.28);
    color:#c4c4c4;
}
.sessions-empty-state{
    padding:2.2rem 1.5rem;
    display:flex;
    flex-direction:column;
    align-items:center;
    text-align:center;
    gap:.9rem;
}
.sessions-empty-state h2{font-size:1.2rem}
.sessions-empty-state p{
    color:var(--text-secondary);
    max-width:34rem;
    line-height:1.6;
}
.sessions-empty-state-error .sessions-empty-icon{background:rgba(255,85,85,.12)}
.sessions-empty-icon{
    width:3.3rem;
    height:3.3rem;
    border-radius:1rem;
    display:grid;
    place-items:center;
    background:rgba(80,250,123,.08);
    font-size:1.55rem;
}
.sessions-empty-actions{
    display:flex;
    gap:.75rem;
    flex-wrap:wrap;
    justify-content:center;
}
.sessions-toast-container{
    position:fixed;
    right:1.5rem;
    bottom:1.5rem;
    display:flex;
    flex-direction:column;
    gap:.75rem;
    z-index:60;
    pointer-events:none;
}
.sessions-toast{
    max-width:min(92vw,24rem);
    padding:.95rem 1rem;
    border-radius:14px;
    background:rgba(22,33,62,.96);
    border:1px solid rgba(80,250,123,.28);
    box-shadow:var(--shadow);
    color:var(--text-primary);
    transform:translateY(10px);
    opacity:0;
    animation:sessions-toast-in .2s ease forwards;
}
.sessions-toast.is-error{border-color:rgba(255,85,85,.32)}
.sessions-toast.is-leaving{animation:sessions-toast-out .2s ease forwards}
@keyframes sessions-toast-in{
    from{opacity:0;transform:translateY(10px)}
    to{opacity:1;transform:translateY(0)}
}
@keyframes sessions-toast-out{
    from{opacity:1;transform:translateY(0)}
    to{opacity:0;transform:translateY(10px)}
}
@media(max-width:640px){
    .sessions-create-actions,
    .session-card-actions{width:100%}
    .sessions-create-actions .primary-button,
    .sessions-create-actions .secondary-button,
    .session-action-button,
    .sessions-create-toggle{width:100%}
}

/* Settings, catalog, and preferences pages */
.settings-grid{
    display:grid;
    gap:1rem;
    grid-template-columns:repeat(auto-fit,minmax(320px,1fr));
}
.settings-card{
    padding:1.25rem;
    display:flex;
    flex-direction:column;
    gap:1rem;
    min-height:220px;
}
.tab-bar{
    display:flex;
    gap:0;
    border-bottom:2px solid #2a2a4a;
    margin-bottom:1.5rem;
}
.tab{
    padding:0.75rem 1.25rem;
    color:var(--text-secondary);
    text-decoration:none;
    border-bottom:2px solid transparent;
    margin-bottom:-2px;
    transition:all .2s;
}
.tab:hover{color:var(--text-primary)}
.tab.active{color:var(--accent);border-bottom-color:var(--accent)}
.section-stack{
    display:flex;
    flex-direction:column;
    gap:1rem;
}
.status-banner{
    border-radius:14px;
    border:1px solid var(--border);
    padding:.9rem 1rem;
    line-height:1.5;
}
.status-banner.is-success{
    background:rgba(80,250,123,.1);
    border-color:rgba(80,250,123,.28);
}
.status-banner.is-error{
    background:rgba(255,85,85,.12);
    border-color:rgba(255,85,85,.28);
}
.status-banner.is-info{
    background:rgba(15,52,96,.6);
}
.loading-panel{
    min-height:160px;
    display:grid;
    place-items:center;
    gap:.8rem;
    text-align:center;
    color:var(--text-secondary);
}
.definition-grid,
.stats-grid,
.detail-columns,
.quick-add-grid{
    display:grid;
    gap:.85rem;
}
.definition-grid{
    grid-template-columns:repeat(auto-fit,minmax(180px,1fr));
}
.definition-item,
.stat-card,
.detail-card{
    background:rgba(15,52,96,.42);
    border:1px solid var(--border);
    border-radius:14px;
    padding:1rem;
}
.definition-label,
.stat-label,
.field-hint,
.preference-meta{
    color:var(--text-secondary);
    font-size:.84rem;
}
.definition-value{
    margin-top:.35rem;
    font-size:1rem;
    font-weight:600;
    word-break:break-word;
}
.stats-grid{
    grid-template-columns:repeat(auto-fit,minmax(170px,1fr));
}
.stat-value{
    font-size:1.5rem;
    font-weight:800;
    margin-top:.35rem;
}
.stat-sub{
    font-size:.85rem;
    color:var(--text-secondary);
    margin-top:.15rem;
}
.data-table th{
    color:var(--text-secondary);
    font-weight:600;
    border-bottom:1px solid var(--border);
}
.data-table td{
    border-bottom:1px solid var(--bg-tertiary);
}
.settings-actions,
.badge-row,
.preference-actions,
.token-list{
    display:flex;
    gap:.65rem;
    flex-wrap:wrap;
    align-items:center;
}
.catalog-list{
    display:flex;
    flex-direction:column;
    gap:1rem;
}
.provider-grid{
    display:grid;
    grid-template-columns:repeat(auto-fit,minmax(280px,1fr));
    gap:.85rem;
}
.provider-card{
    background:rgba(15,52,96,.42);
    border:1px solid var(--border);
    border-radius:14px;
    padding:1.1rem;
    position:relative;
}
.provider-card.provider-default{
    border-color:var(--accent);
    box-shadow:0 0 0 1px var(--accent);
}
.provider-header{
    display:flex;
    justify-content:space-between;
    align-items:center;
    margin-bottom:.75rem;
}
.provider-name{
    font-size:1.05rem;
    font-weight:700;
    text-transform:capitalize;
}
.provider-meta{
    display:flex;
    flex-direction:column;
    gap:.35rem;
    margin-bottom:.75rem;
}
.provider-meta-row{
    display:flex;
    gap:.5rem;
    align-items:baseline;
}
.provider-meta-label{
    color:var(--text-secondary);
    font-size:.84rem;
    min-width:60px;
}
.provider-meta-value{
    font-size:.92rem;
    font-weight:600;
    word-break:break-all;
}
.provider-actions{
    display:flex;
    gap:.5rem;
    flex-wrap:wrap;
    margin-top:.5rem;
}
.catalog-card{
    padding:1.15rem 1.25rem;
}
.catalog-trigger{
    display:block;
    width:100%;
    background:none;
    border:none;
    color:inherit;
    text-align:left;
}
.catalog-summary{
    display:flex;
    align-items:flex-start;
    justify-content:space-between;
    gap:1rem;
}
.catalog-title{
    font-size:1.08rem;
    margin-bottom:.35rem;
}
.catalog-description{
    color:var(--text-secondary);
    line-height:1.55;
}
.badge{
    display:inline-flex;
    align-items:center;
    gap:.4rem;
    border-radius:999px;
    padding:.34rem .72rem;
    font-size:.78rem;
    border:1px solid var(--border);
    background:rgba(255,255,255,.04);
}
.badge.is-accent{
    color:var(--accent);
    border-color:rgba(80,250,123,.2);
    background:rgba(80,250,123,.08);
}
.badge.is-muted{
    color:var(--text-secondary);
}
.autonomy-stars{
    display:inline-flex;
    gap:.18rem;
}
.autonomy-star{
    color:rgba(224,224,224,.28);
    font-size:.9rem;
}
.autonomy-star.is-filled{
    color:var(--accent);
}
.expand-icon{
    color:var(--text-secondary);
    font-size:1.1rem;
    padding-top:.15rem;
}
.expand-panel{
    margin-top:1rem;
    padding-top:1rem;
    border-top:1px solid var(--border);
    display:flex;
    flex-direction:column;
    gap:1rem;
}
.detail-columns{
    grid-template-columns:repeat(auto-fit,minmax(250px,1fr));
}
.detail-card h3{
    font-size:.96rem;
    margin-bottom:.45rem;
}
.detail-card p{
    color:var(--text-secondary);
    line-height:1.55;
}
.detail-code,
.inline-code{
    border-radius:12px;
    background:rgba(5,7,18,.5);
    border:1px solid var(--border);
    padding:.85rem 1rem;
    font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;
}
.detail-code{
    color:#d8f9df;
    white-space:pre-wrap;
    word-break:break-word;
    max-height:220px;
    overflow:auto;
}
.inline-code{
    display:inline-block;
    padding:.18rem .45rem;
    font-size:.82rem;
}
.token-list{
    list-style:none;
    padding:0;
}
.token{
    display:inline-flex;
    align-items:center;
    border-radius:999px;
    padding:.32rem .72rem;
    background:rgba(80,250,123,.08);
    border:1px solid rgba(80,250,123,.16);
    color:var(--accent);
    font-size:.8rem;
}
.agent-config-grid{
    display:grid;
    gap:1rem;
}
.agent-config-form{
    display:flex;
    flex-direction:column;
    gap:1rem;
}
.agent-config-form .field-row{
    display:flex;
    gap:1rem;
    flex-wrap:wrap;
}
.agent-config-form .field-row>.field{
    flex:1 1 220px;
}
.agent-notice{
    border-radius:14px;
    border:1px solid rgba(255,184,108,.28);
    background:rgba(255,184,108,.08);
    padding:.75rem 1rem;
    color:#ffcf88;
    font-size:.88rem;
    line-height:1.5;
}
.progress-track{
    width:100%;
    height:10px;
    background:rgba(255,255,255,.06);
    border-radius:999px;
    overflow:hidden;
    border:1px solid var(--border);
}
.progress-fill{
    height:100%;
    border-radius:999px;
    transition:width .3s ease;
}
.progress-fill.is-ok{background:var(--accent)}
.progress-fill.is-warn{background:#ffb86c}
.progress-fill.is-danger{background:#ff5555}
.toggle-row{
    display:flex;
    align-items:center;
    justify-content:space-between;
    padding:.75rem 1rem;
    background:rgba(15,52,96,.42);
    border:1px solid var(--border);
    border-radius:14px;
}
.toggle-row label{
    display:flex;
    flex-direction:column;
    gap:.15rem;
}
.toggle-row label span:first-child{
    font-weight:600;
    color:var(--text-primary);
}
.toggle-row label span:last-child{
    font-size:.82rem;
    color:var(--text-secondary);
}
.toggle-switch{
    position:relative;
    width:48px;
    height:26px;
    flex-shrink:0;
}
.toggle-switch input{
    opacity:0;
    width:0;
    height:0;
    position:absolute;
}
.toggle-slider{
    position:absolute;
    inset:0;
    background:rgba(255,255,255,.08);
    border-radius:999px;
    border:1px solid var(--border);
    cursor:pointer;
    transition:background .2s;
}
.toggle-slider::before{
    content:'';
    position:absolute;
    left:3px;
    top:3px;
    width:18px;
    height:18px;
    border-radius:50%;
    background:#888;
    transition:transform .2s,background .2s;
}
.toggle-switch input:checked+.toggle-slider{
    background:rgba(80,250,123,.18);
    border-color:rgba(80,250,123,.4);
}
.toggle-switch input:checked+.toggle-slider::before{
    transform:translateX(22px);
    background:var(--accent);
}
.orch-settings{
    margin-bottom:1.5rem;
}
.orch-settings .orch-bar{
    display:flex;
    gap:1rem;
    align-items:flex-end;
    flex-wrap:wrap;
}
.orch-settings .orch-bar>.field{
    flex:0 1 220px;
}
.agent-card-badges{
    display:flex;
    gap:.4rem;
    flex-wrap:wrap;
    align-items:center;
    margin-top:.35rem;
}
.agent-card-actions{
    display:flex;
    align-items:center;
    gap:.5rem;
    flex-shrink:0;
}
.table-shell{
    overflow:auto;
    border:1px solid var(--border);
    border-radius:14px;
}
.preference-table{
    width:100%;
    border-collapse:collapse;
    min-width:720px;
}
.preference-table th,
.preference-table td{
    padding:.85rem .75rem;
    text-align:left;
    vertical-align:top;
    border-bottom:1px solid var(--border);
}
.preference-table th{
    background:rgba(255,255,255,.02);
    color:var(--text-secondary);
    font-size:.78rem;
    letter-spacing:.08em;
    text-transform:uppercase;
}
.preference-table tr:last-child td{
    border-bottom:none;
}
.preference-key{
    font-weight:700;
    word-break:break-word;
}
.preference-group{
    padding:1.25rem;
}
.preference-group .section-heading{
    margin-bottom:0;
}
.quick-add-grid{
    grid-template-columns:repeat(auto-fit,minmax(180px,1fr));
}
.quick-add-button{
    min-height:unset;
    padding:.95rem 1rem;
    text-align:left;
    justify-content:flex-start;
    display:flex;
    flex-direction:column;
    align-items:flex-start;
    gap:.35rem;
}
.quick-add-button strong{
    font-size:.95rem;
}
.empty-state{
    padding:2rem 1.25rem;
    text-align:center;
    color:var(--text-secondary);
}
.empty-state strong{
    display:block;
    color:var(--text-primary);
    font-size:1.02rem;
    margin-bottom:.45rem;
}
.assistant-note{
    color:var(--text-secondary);
    line-height:1.6;
}
.token-management-layout{
    display:flex;
    flex-direction:column;
    gap:1rem;
}
.token-management-top{
    display:grid;
    gap:1rem;
    grid-template-columns:repeat(auto-fit,minmax(280px,1fr));
}
.token-summary-grid{
    display:grid;
    gap:.85rem;
    grid-template-columns:repeat(auto-fit,minmax(140px,1fr));
}
.token-summary-card{
    background:rgba(15,52,96,.42);
    border:1px solid var(--border);
    border-radius:14px;
    padding:1rem;
}
.token-summary-card strong{
    display:block;
    font-size:1.15rem;
    margin-top:.35rem;
}
.token-warning{
    color:#ffcf88;
    line-height:1.5;
}
.token-status-badge{
    display:inline-flex;
    align-items:center;
    border-radius:999px;
    padding:.34rem .72rem;
    font-size:.78rem;
    border:1px solid transparent;
}
.token-status-badge.is-active{
    color:var(--accent);
    border-color:rgba(80,250,123,.22);
    background:rgba(80,250,123,.08);
}
.token-status-badge.is-revoked{
    color:#ffb3b3;
    border-color:rgba(255,120,120,.28);
    background:rgba(255,120,120,.09);
}
@media(max-width:900px){
    .settings-grid,
    .detail-columns{
        grid-template-columns:1fr;
    }
}
@media(max-width:640px){
    .catalog-summary{
        flex-direction:column;
    }
    .settings-actions,
    .preference-actions{
        width:100%;
    }
    .preference-table{
        min-width:640px;
    }
}
/* ── Timers page ── */
.timers-toolbar{
    display:flex;
    justify-content:space-between;
    align-items:center;
    flex-wrap:wrap;
    gap:.75rem;
    margin-bottom:1rem;
}
.timers-table-wrap{
    overflow-x:auto;
    border:1px solid var(--border);
    border-radius:14px;
    background:var(--bg-surface);
}
.timers-table{
    width:100%;
    border-collapse:collapse;
    font-size:.875rem;
}
.timers-table th,
.timers-table td{
    padding:.65rem .85rem;
    text-align:left;
    white-space:nowrap;
    border-bottom:1px solid var(--border);
}
.timers-table th{
    color:var(--text-secondary);
    font-weight:600;
    font-size:.78rem;
    text-transform:uppercase;
    letter-spacing:.04em;
    background:rgba(15,52,96,.42);
}
.timers-table tbody tr:last-child td{
    border-bottom:none;
}
.timers-table tbody tr:hover{
    background:rgba(255,255,255,.03);
}
.timer-name{
    font-weight:600;
    color:var(--text-primary);
}
.timer-description{
    font-weight:400;
    color:var(--text-secondary);
    font-size:.8rem;
    white-space:normal;
    max-width:200px;
}
.timer-actions{
    display:flex;
    gap:.4rem;
    align-items:center;
}
.timer-actions button{
    padding:.3rem .6rem;
    font-size:.78rem;
    border-radius:8px;
    cursor:pointer;
    border:1px solid var(--border);
    background:var(--bg-surface);
    color:var(--text-primary);
    transition:background .15s,border-color .15s;
}
.timer-actions button:hover{
    background:rgba(255,255,255,.08);
    border-color:var(--accent);
}
.timer-actions button.is-danger:hover{
    border-color:#ff5555;
    color:#ff5555;
}
.timer-actions button.is-run{
    border-color:rgba(80,250,123,.4);
    color:var(--accent);
}
.timer-badge{
    display:inline-block;
    padding:.18rem .55rem;
    border-radius:999px;
    font-size:.75rem;
    font-weight:600;
    letter-spacing:.02em;
}
.timer-badge.is-enabled{
    background:rgba(80,250,123,.15);
    color:var(--accent);
}
.timer-badge.is-disabled{
    background:rgba(255,255,255,.06);
    color:var(--text-secondary);
}
.timer-badge.is-failed{
    background:rgba(255,85,85,.15);
    color:#ff5555;
}
.timer-badge.is-success{
    background:rgba(80,250,123,.15);
    color:var(--accent);
}
.timer-badge.is-running{
    background:rgba(189,147,249,.15);
    color:#bd93f9;
}
/* Timer modal */
.timer-modal-overlay{
    position:fixed;
    inset:0;
    background:rgba(0,0,0,.6);
    display:flex;
    align-items:center;
    justify-content:center;
    z-index:9000;
    padding:1rem;
}
.timer-modal{
    background:var(--bg-panel);
    border:1px solid var(--border);
    border-radius:16px;
    width:100%;
    max-width:600px;
    max-height:85vh;
    overflow-y:auto;
    padding:1.5rem;
}
.timer-modal h2{
    margin:0 0 1rem 0;
    font-size:1.15rem;
}
.timer-form-group{
    margin-bottom:.85rem;
}
.timer-form-group label{
    display:block;
    font-size:.82rem;
    font-weight:600;
    color:var(--text-secondary);
    margin-bottom:.3rem;
}
.timer-form-group input[type="text"],
.timer-form-group input[type="number"],
.timer-form-group input[type="datetime-local"],
.timer-form-group textarea,
.timer-form-group select{
    width:100%;
    padding:.55rem .75rem;
    border:1px solid var(--border);
    border-radius:10px;
    background:var(--bg-surface);
    color:var(--text-primary);
    font-size:.875rem;
    font-family:inherit;
    box-sizing:border-box;
}
.timer-form-group textarea{
    min-height:70px;
    resize:vertical;
}
.timer-form-row{
    display:flex;
    gap:.75rem;
    flex-wrap:wrap;
}
.timer-form-row .timer-form-group{
    flex:1;
    min-width:120px;
}
.timer-radio-group{
    display:flex;
    gap:1rem;
    padding:.35rem 0;
}
.timer-radio-group label{
    display:inline-flex;
    align-items:center;
    gap:.35rem;
    font-size:.875rem;
    color:var(--text-primary);
    cursor:pointer;
    font-weight:400;
}
.timer-cron-presets{
    margin-top:.35rem;
}
.timer-cron-presets select{
    width:100%;
    padding:.45rem .65rem;
    border:1px solid var(--border);
    border-radius:10px;
    background:var(--bg-surface);
    color:var(--text-secondary);
    font-size:.8rem;
}
.timer-modal-actions{
    display:flex;
    justify-content:flex-end;
    gap:.65rem;
    margin-top:1.25rem;
    padding-top:1rem;
    border-top:1px solid var(--border);
}
@media(max-width:768px){
    .timers-table th:nth-child(n+5),
    .timers-table td:nth-child(n+5){
        display:none;
    }
    .timer-form-row{
        flex-direction:column;
    }
}
.model-status-indicator{
    padding:0.75rem 1rem;
    border-radius:8px;
    margin-bottom:0.75rem;
    font-size:0.9rem;
    line-height:1.5;
}
.model-status-loading{
    background:var(--surface);
    display:flex;
    align-items:center;
    gap:0.5rem;
}
.model-status-success{
    background:rgba(80,250,123,0.1);
    border:1px solid rgba(80,250,123,0.3);
}
.model-status-error{
    background:rgba(255,85,85,0.1);
    border:1px solid rgba(255,85,85,0.3);
}
.restart-section{
    margin-top:1.5rem;
    padding-top:1.5rem;
    border-top:1px solid var(--border);
}
.restart-status{
    padding:0.75rem 1rem;
    border-radius:8px;
    margin-top:0.75rem;
    font-size:0.9rem;
}
.restart-status-restarting{
    background:rgba(255,184,108,0.1);
    border:1px solid rgba(255,184,108,0.3);
    display:flex;
    align-items:center;
    gap:0.5rem;
}
.restart-status-polling{
    background:rgba(80,250,123,0.1);
    border:1px solid rgba(80,250,123,0.3);
    display:flex;
    align-items:center;
    gap:0.5rem;
}
</style>
</head>
<body>
<!-- Application shell -->
<div class="app-shell">
    <!-- Sidebar navigation -->
    <aside class="sidebar" id="sidebar" aria-label="Primary navigation">
        <div class="sidebar-header">
            <div class="sidebar-brand">
                <div class="sidebar-mark" aria-hidden="true">OV</div>
                <div class="sidebar-brand-text">
                    <div class="sidebar-brand-title" id="sidebarBrandTitle">OpenVEPA</div>
                    <div class="sidebar-brand-subtitle">WebUI Navigation</div>
                </div>
            </div>
        </div>
        <div class="sidebar-caption">Workspace</div>
        <nav aria-label="WebUI sections">
            <ul class="nav-list" id="navList"></ul>
        </nav>
        <div class="sidebar-footer">
            <div class="theme-picker-label">Theme</div>
            <div class="theme-picker" id="themePicker">
                <button type="button" class="theme-btn" data-theme-pref="light" title="Light theme">☀️</button>
                <button type="button" class="theme-btn" data-theme-pref="dark" title="Dark theme">🌙</button>
                <button type="button" class="theme-btn" data-theme-pref="system" title="System theme">💻</button>
            </div>
        </div>
    </aside>

    <!-- Main content -->
    <div class="main-area">
        <!-- Top bar -->
        <header class="topbar">
            <div class="topbar-left">
                <button type="button" class="menu-toggle" id="sidebarToggle" aria-label="Toggle navigation" aria-controls="sidebar" aria-expanded="true">
                    <span class="menu-bars" aria-hidden="true">☰</span>
                </button>
                <div class="topbar-brand">
                    <div class="topbar-brand-row">
                        <span class="topbar-title" id="topbarTitle">OpenVEPA</span>
                        <span class="topbar-page" id="pageTitle">Home</span>
                    </div>
                    <div class="topbar-subtitle" id="pageSubtitle">Chat with your assistant in the main workspace.</div>
                </div>
            </div>
            <div class="topbar-status">
                <span class="status-dot" aria-hidden="true"></span>
                <span>WebUI shell ready</span>
            </div>
        </header>

        <!-- Route outlet -->
        <main class="page-content" id="pageContainer" tabindex="-1"></main>
    </div>
</div>
<div class="sidebar-overlay" id="sidebarOverlay" hidden></div>

<script>
(function(){
    'use strict';

    /* ── Theme management ── */
    function getSystemTheme(){
        return window.matchMedia('(prefers-color-scheme:dark)').matches ? 'dark' : 'light';
    }

    function applyTheme(preference){
        var theme = preference === 'system' ? getSystemTheme() : preference;
        document.documentElement.setAttribute('data-theme', theme);
        try { localStorage.setItem('openvepa-theme', preference); } catch(e){}
        syncThemePicker(preference);
    }

    function syncThemePicker(preference){
        var btns = document.querySelectorAll('.theme-btn');
        for(var i = 0; i < btns.length; i++){
            if(btns[i].getAttribute('data-theme-pref') === preference){
                btns[i].classList.add('is-active');
            } else {
                btns[i].classList.remove('is-active');
            }
        }
    }

    function initTheme(){
        var saved = 'system';
        try { saved = localStorage.getItem('openvepa-theme') || 'system'; } catch(e){}
        applyTheme(saved);
        window.matchMedia('(prefers-color-scheme:dark)').addEventListener('change', function(){
            var pref = 'system';
            try { pref = localStorage.getItem('openvepa-theme') || 'system'; } catch(e){}
            if(pref === 'system') applyTheme('system');
        });
    }

    initTheme();

    var routes = [
        {
            hash:'#/home',
            title:'Home',
            icon:'🏠',
            description:'Chat with your assistant in the main workspace.',
            render:renderHomePage,
            afterRender:initHomePage
        },
        {
            hash:'#/sessions',
            title:'Sessions',
            icon:'💬',
            description:'Browse and manage saved assistant conversations.',
            render:renderSessionsPage
        },
        {
            hash:'#/llm-settings',
            title:'LLM Settings',
            icon:'🤖',
            description:'Configure providers, models, and connection defaults.',
            render:renderLlmSettingsPage
        },
        {
            hash:'#/usage',
            title:'Usage',
            icon:'📊',
            description:'LLM usage statistics and cost tracking.',
            render:renderUsagePage
        },
        {
            hash:'#/agents',
            title:'Agents',
            icon:'🕵️',
            description:'Inspect available agents and manage their lifecycle.',
            render:renderAgentsPage
        },
        {
            hash:'#/skills',
            title:'Skills',
            icon:'🔧',
            description:'Review installed skills and their configuration.',
            render:renderSkillsPage
        },
        {
            hash:'#/channels',
            title:'Channels',
            icon:'📡',
            description:'Configure external messaging connectors.',
            render:renderChannelsPage
        },
        {
            hash:'#/timers',
            title:'Timers',
            icon:'⏰',
            description:'Manage scheduled tasks and automated prompts.',
            render:renderTimersPage
        },
        {
            hash:'#/user-settings',
            title:'User Settings',
            icon:'👤',
            description:'Customize profile, preferences, and personal defaults.',
            render:renderUserSettingsPage
        }
    ];

    var routeMap = {};
    for(var i=0;i<routes.length;i++){
        routeMap[routes[i].hash] = routes[i];
    }

    var navList = document.getElementById('navList');
    var pageContainer = document.getElementById('pageContainer');
    var pageTitle = document.getElementById('pageTitle');
    var pageSubtitle = document.getElementById('pageSubtitle');
    var sidebarToggle = document.getElementById('sidebarToggle');
    var sidebarOverlay = document.getElementById('sidebarOverlay');
    var mobileMedia = window.matchMedia('(max-width: 900px)');
    var storageKeys = {
        token:'openvepa_token',
        tokenId:'openvepa_token_id',
        sessionId:'openvepa_active_session_id'
    };
    var homeState = {
        status:null,
        statusLoaded:false,
        statusLoading:false,
        statusError:'',
        token:'',
        tokenDraft:'',
        authError:'',
        isBootstrapping:false,
        bootstrapMessage:'',
        bootstrapMessageKind:'info',
        sessions:[],
        sessionsLoaded:false,
        sessionsError:'',
        selectedSessionId:'',
        messages:[],
        messagesLoadedFor:'',
        isLoadingSessions:false,
        isLoadingMessages:false,
        isCreatingSession:false,
        isSending:false,
        requestCounter:0,
        eventsBound:false
    };

    try {
        homeState.token = localStorage.getItem(storageKeys.token) || '';
        homeState.tokenDraft = homeState.token;
        homeState.selectedSessionId = localStorage.getItem(storageKeys.sessionId) || '';
    } catch (error) {
        homeState.token = '';
        homeState.tokenDraft = '';
        homeState.selectedSessionId = '';
    }

    // Future enhancement: stream assistant responses over SignalR when the client library
    // is bundled or loaded from a trusted source.
    // async function streamMessageWithSignalR(sessionId, message){
    //     var connection = new signalR.HubConnectionBuilder()
    //         .withUrl('/hub/assistant?access_token=' + encodeURIComponent(homeState.token))
    //         .withAutomaticReconnect()
    //         .build();
    //
    //     await connection.start();
    //
    //     connection.stream('StreamMessage', sessionId, message).subscribe({
    //         next:function(chunk){
    //             // Append each chunk to an in-progress assistant bubble.
    //         },
    //         complete:function(){
    //             // Finalize the streamed assistant response.
    //         },
    //         error:function(error){
    //             // Surface streaming errors in the chat transcript.
    //         }
    //     });
    // }

    function renderNavigation(){
        var html = '';
        for(var i=0;i<routes.length;i++){
            var route = routes[i];
            html += `
<li>
    <a class="nav-link" href="${route.hash}" data-route="${route.hash}" title="${route.title}" aria-label="${route.title}">
        <span class="nav-icon" aria-hidden="true">${route.icon}</span>
        <span class="nav-copy">
            <span class="nav-title">${route.title}</span>
            <span class="nav-description">${route.description}</span>
        </span>
    </a>
</li>`;
        }
        navList.innerHTML = html;
    }

    function getRoute(){
        var hash = window.location.hash;
        if(hash === '#/user-settings' || hash.indexOf('#/user-settings/') === 0){
            return routeMap['#/user-settings'];
        }
        if(hash === '#/channels' || hash.indexOf('#/channels/') === 0){
            return routeMap['#/channels'];
        }
        if(hash === '#/agents' || hash.indexOf('#/agents/') === 0){
            return routeMap['#/agents'];
        }
        return routeMap[hash] || routeMap['#/home'];
    }

    function getActiveSettingsTab(){
        var hash = window.location.hash;
        var prefix = '#/user-settings/';
        if(hash.indexOf(prefix) === 0){
            var tab = hash.substring(prefix.length).split('/')[0];
            if(tab === 'profile' || tab === 'preferences' || tab === 'tokens' || tab === 'system'){
                return tab;
            }
        }
        return 'profile';
    }

    function updateActiveNav(routeHash){
        var links = navList.querySelectorAll('.nav-link');
        for(var i=0;i<links.length;i++){
            var isActive = links[i].getAttribute('data-route') === routeHash;
            links[i].classList.toggle('is-active', isActive);
            if(isActive){
                links[i].setAttribute('aria-current', 'page');
            } else {
                links[i].removeAttribute('aria-current');
            }
        }
    }

    function renderRoute(){
        if(window.location.hash === '#/user-settings'){
            window.location.hash = '#/user-settings/profile';
            return;
        }
        var route = getRoute();
        pageTitle.textContent = route.title;
        pageSubtitle.textContent = route.description;
        pageContainer.innerHTML = route.render();
        if(route.afterRender){
            route.afterRender();
        }
        updateActiveNav(route.hash);
        if(isMobile()){
            closeMobileSidebar();
        }
    }

    // Home chat page lifecycle
    function initHomePage(){
        if(!homeState.eventsBound){
            bindHomePageEvents();
            homeState.eventsBound = true;
        }

        updateHomePage();

        if(!homeState.statusLoaded && !homeState.statusLoading){
            loadServerStatus();
            return;
        }

        if(canUseChat() && !homeState.sessionsLoaded && !homeState.isLoadingSessions){
            loadHomeSessions(true);
        }
    }

    function bindHomePageEvents(){
        pageContainer.addEventListener('submit', function(event){
            var form = event.target;
            if(!form || form.id !== 'tokenForm'){
                return;
            }

            event.preventDefault();
            var tokenInput = document.getElementById('tokenInput');
            var nextToken = tokenInput ? tokenInput.value.trim() : '';
            homeState.tokenDraft = nextToken;

            if(!nextToken){
                homeState.authError = 'Enter a bearer token to continue.';
                updateHomePage();
                return;
            }

            homeState.token = nextToken;
            homeState.authError = '';
            homeState.bootstrapMessage = '';
            homeState.sessions = [];
            homeState.messages = [];
            homeState.sessionsLoaded = false;
            homeState.messagesLoadedFor = '';
            storeToken(nextToken);
            storeTokenId('');
            updateHomePage();
            loadHomeSessions(true);
        });

        pageContainer.addEventListener('click', function(event){
            var target = event.target;
            var button = target && target.closest ? target.closest('button') : null;
            if(!button){
                return;
            }

            if(button.id === 'newSessionButton' || button.id === 'emptyNewSessionButton'){
                event.preventDefault();
                createSession();
                return;
            }

            if(button.id === 'chatSendButton'){
                event.preventDefault();
                sendMessage();
                return;
            }

            if(button.id === 'bootstrapTokenButton'){
                event.preventDefault();
                createBootstrapToken();
                return;
            }

            if(button.id === 'changeTokenButton'){
                event.preventDefault();
                clearToken();
            }
        });

        pageContainer.addEventListener('change', function(event){
            var target = event.target;
            if(target && target.id === 'chatSessionSelect'){
                selectSession(target.value);
            }
        });

        pageContainer.addEventListener('input', function(event){
            var target = event.target;
            if(!target){
                return;
            }

            if(target.id === 'tokenInput'){
                homeState.tokenDraft = target.value;
                if(homeState.authError){
                    homeState.authError = '';
                    var authBanner = document.querySelector('#chatAuthPanel .error-banner');
                    if(authBanner){
                        authBanner.remove();
                    }
                }
                return;
            }

            if(target.id === 'chatInput'){
                updateComposerState();
            }
        });

        pageContainer.addEventListener('keydown', function(event){
            var target = event.target;
            if(target && target.id === 'chatInput' && event.key === 'Enter' && !event.shiftKey){
                event.preventDefault();
                sendMessage();
            }
        });
    }

    // Home chat rendering
    function updateHomePage(){
        var statusPill = document.getElementById('homeStatusPill');
        if(!statusPill){
            return;
        }

        var statusText = document.getElementById('chatStatusText');
        var authPanel = document.getElementById('chatAuthPanel');
        var composerPanel = document.getElementById('chatComposerPanel');
        var loadingIndicator = document.getElementById('chatLoadingIndicator');
        var messagesHost = document.getElementById('chatMessages');
        var busy = homeState.statusLoading || homeState.isLoadingSessions || homeState.isLoadingMessages || homeState.isSending || homeState.isCreatingSession || homeState.isBootstrapping;
        var showComposer = canUseChat();

        statusPill.textContent = getHomeStatusPillText();
        if(statusText){
            statusText.textContent = getHomeStatusText();
        }

        if(loadingIndicator){
            loadingIndicator.hidden = !busy;
        }

        if(messagesHost){
            messagesHost.setAttribute('aria-busy', busy ? 'true' : 'false');
        }

        if(authPanel){
            authPanel.hidden = showComposer;
            renderAuthPanel(authPanel);
        }

        if(composerPanel){
            composerPanel.hidden = !showComposer;
        }

        renderSessionSelect();
        renderMessages();
        updateComposerState();
    }

    function renderAuthPanel(authPanel){
        if(!authPanel){
            return;
        }

        if(homeState.statusLoading){
            authPanel.innerHTML = `
<div class="section-heading">
    <div>
        <h2>Checking WebUI status</h2>
        <p>Contacting <code>/api/status</code> before enabling authenticated chat actions.</p>
    </div>
</div>
<div class="helper-note">Please wait while the dashboard confirms the server is ready.</div>`;
            return;
        }

        if(homeState.statusError){
            authPanel.innerHTML = `
<div class="section-heading">
    <div>
        <h2>Status check failed</h2>
        <p>The WebUI could not reach the local server status endpoint.</p>
    </div>
</div>
${renderErrorBanner(homeState.statusError)}`;
            return;
        }

        if(homeState.status && homeState.status.setupComplete === false){
            authPanel.innerHTML = `
<div class="section-heading">
    <div>
        <h2>Setup required</h2>
        <p>OpenVEPA still needs initial configuration before chat sessions can be used.</p>
    </div>
</div>
<div class="helper-note">Complete the setup flow at <a href="/init/setupwizard">/init/setupwizard</a>, then return here to continue.</div>`;
            return;
        }

        authPanel.innerHTML = `
<div class="section-heading">
    <div>
        <h2>Bearer token required</h2>
        <p>Use a token to unlock chat, sessions, and settings in this browser.</p>
    </div>
</div>
${renderErrorBanner(homeState.authError)}
${homeState.bootstrapMessage ? renderStatusBanner(homeState.bootstrapMessage, homeState.bootstrapMessageKind || 'info') : ''}
<div class="auth-cta-stack">
    <div class="auth-create-card">
        <div>
            <h3>Create your first token</h3>
            <p class="helper-note">On a brand-new OpenVEPA install, the browser can bootstrap a token automatically.</p>
        </div>
        <div class="auth-create-actions">
            <button type="button" class="primary-button" id="bootstrapTokenButton" ${homeState.isBootstrapping ? 'disabled' : ''}>${homeState.isBootstrapping ? 'Creating…' : 'Create your first token'}</button>
            <span class="helper-note">Calls <code>POST /api/tokens/bootstrap</code> with <code>{ "name": "web-browser" }</code>.</span>
        </div>
    </div>
    <form id="tokenForm" class="auth-form">
        <label class="field" for="tokenInput">
            <span>Bearer token</span>
            <input id="tokenInput" class="control" type="password" placeholder="Paste Bearer token" autocomplete="off" spellcheck="false" value="${escapeHtml(homeState.tokenDraft)}">
        </label>
        <div class="auth-manual-help">
            <p class="helper-note">If you're running in Docker, you can create a token via:</p>
            <pre class="detail-code docker-command">docker exec openvepa openvepa token create "my-token"</pre>
            <p class="helper-note">Then paste the token above.</p>
        </div>
        <div class="toolbar-actions">
            <button type="submit" class="primary-button">Save token</button>
            <a class="text-link" href="#/user-settings/tokens">Manage tokens in User Settings</a>
        </div>
        <p class="helper-note">Once saved, the dashboard calls <code>/api/sessions</code>, <code>/api/sessions/{id}/messages</code>, and token management APIs with an <code>Authorization: Bearer &lt;token&gt;</code> header.</p>
    </form>
</div>`;
    }

    function renderSessionSelect(){
        var select = document.getElementById('chatSessionSelect');
        if(!select){
            return;
        }

        if(!canUseChat()){
            select.innerHTML = '<option value="">Authentication required</option>';
            select.disabled = true;
            return;
        }

        if(homeState.isLoadingSessions && homeState.sessions.length === 0){
            select.innerHTML = '<option value="">Loading sessions…</option>';
            select.disabled = true;
            return;
        }

        if(homeState.sessions.length === 0){
            select.innerHTML = '<option value="">No sessions yet</option>';
            select.disabled = homeState.isCreatingSession || homeState.isSending;
            return;
        }

        var html = '';
        for(var i=0;i<homeState.sessions.length;i++){
            var session = homeState.sessions[i];
            html += `<option value="${escapeHtml(session.id)}">${escapeHtml(formatSessionOption(session))}</option>`;
        }

        select.innerHTML = html;
        select.value = homeState.selectedSessionId || homeState.sessions[0].id;
        select.disabled = homeState.isLoadingSessions || homeState.isLoadingMessages || homeState.isCreatingSession || homeState.isSending;
    }

    function renderMessages(){
        var host = document.getElementById('chatMessages');
        if(!host){
            return;
        }

        if(homeState.statusLoading){
            host.innerHTML = renderEmptyState('Checking server status', 'Loading the WebUI status endpoint before enabling authenticated chat.');
            return;
        }

        if(homeState.statusError){
            host.innerHTML = `
${renderErrorBanner(homeState.statusError)}
${renderEmptyState('Server unavailable', 'Fix the server status issue and refresh the page to continue.')}`;
            return;
        }

        if(homeState.status && homeState.status.setupComplete === false){
            host.innerHTML = renderEmptyState('Setup is not complete', 'Finish the setup wizard, then return to Home to start using chat sessions.');
            return;
        }

        if(!homeState.token){
            host.innerHTML = renderEmptyState('Authentication required', 'Create or paste a bearer token below to load assistant sessions in this browser.');
            return;
        }

        if(homeState.isLoadingSessions && homeState.sessions.length === 0){
            host.innerHTML = renderEmptyState('Loading sessions', 'Fetching your saved sessions from the REST API.');
            return;
        }

        if(homeState.sessionsError && homeState.sessions.length === 0){
            host.innerHTML = `
${renderErrorBanner(homeState.sessionsError)}
${renderEmptyState('Unable to load sessions', 'Review the error above, then try again with a valid token.')}`;
            return;
        }

        if(homeState.sessions.length === 0){
            host.innerHTML = renderEmptyState(
                'No sessions yet',
                'Create your first session to start chatting with the assistant.',
                '<button type="button" id="emptyNewSessionButton" class="primary-button">+ New Session</button>');
            return;
        }

        if(!homeState.selectedSessionId){
            host.innerHTML = renderEmptyState('Select a session', 'Choose a session from the dropdown above to review its messages.');
            return;
        }

        if(homeState.isLoadingMessages){
            host.innerHTML = renderEmptyState('Loading conversation', 'Retrieving message history for the selected session.');
            return;
        }

        if(homeState.messages.length === 0){
            host.innerHTML = renderEmptyState('Start the conversation', 'Send the first message for this session and the assistant response will appear here.');
            return;
        }

        var html = '';
        for(var i=0;i<homeState.messages.length;i++){
            html += renderMessage(homeState.messages[i]);
        }

        if(homeState.isSending){
            html += `
<div class="message-row is-assistant">
    <article class="message assistant">
        <div class="message-header">
            <span class="message-label">Assistant</span>
            <span class="message-timestamp">Thinking…</span>
        </div>
        <div class="loading-indicator" aria-hidden="true"><span></span><span></span><span></span></div>
    </article>
</div>`;
        }

        host.innerHTML = html;
        scrollChatToBottom();
    }

    function renderMessage(message){
        var roleName = normalizeRole(message && message.role);
        var bubbleRole = roleName === 'tool' ? 'tool' : roleName;
        var rowRole = roleName === 'user' ? 'user' : (roleName === 'system' ? 'system' : 'assistant');
        var usageText = formatUsage(message ? message.usage : null);
        var metaHtml = usageText ? `<div class="message-meta"><span class="message-usage">${escapeHtml(usageText)}</span></div>` : '';

        return `
<div class="message-row is-${rowRole}">
    <article class="message ${bubbleRole}">
        <div class="message-header">
            <span class="message-label">${escapeHtml(getRoleLabel(roleName))}</span>
            <time class="message-timestamp" datetime="${escapeHtml(message && message.timestamp ? message.timestamp : '')}">${escapeHtml(formatTimestamp(message && message.timestamp))}</time>
        </div>
        <div class="message-body">${formatMessageContent(message && message.content ? message.content : '')}</div>
        ${metaHtml}
    </article>
</div>`;
    }

    function renderEmptyState(title, description, actionHtml){
        return `
<div class="message-empty">
    <div class="message-empty-copy">
        <h3>${escapeHtml(title)}</h3>
        <p>${escapeHtml(description)}</p>
        ${actionHtml ? `<div class="empty-action">${actionHtml}</div>` : ''}
    </div>
</div>`;
    }

    function updateComposerState(){
        var input = document.getElementById('chatInput');
        var sendButton = document.getElementById('chatSendButton');
        var newSessionButton = document.getElementById('newSessionButton');
        var changeTokenButton = document.getElementById('changeTokenButton');
        var helperText = document.getElementById('chatHelperText');
        var canCompose = canUseChat() && !!homeState.selectedSessionId && !homeState.isLoadingMessages;
        var hasMessage = input ? input.value.trim().length > 0 : false;

        if(input){
            input.disabled = !canCompose || homeState.isSending;
        }

        if(sendButton){
            sendButton.disabled = !canCompose || !hasMessage || homeState.isSending;
            sendButton.textContent = homeState.isSending ? '…' : '▶';
        }

        if(newSessionButton){
            newSessionButton.disabled = !canUseChat() || homeState.isLoadingSessions || homeState.isCreatingSession || homeState.isSending;
            newSessionButton.textContent = homeState.isCreatingSession ? 'Creating…' : '+ New Session';
        }

        if(changeTokenButton){
            changeTokenButton.disabled = homeState.isCreatingSession || homeState.isSending || homeState.isLoadingMessages;
        }

        if(helperText){
            helperText.textContent = getComposerHelperText(canCompose);
        }
    }

    // Home chat API calls
    async function loadServerStatus(){
        homeState.statusLoading = true;
        homeState.statusError = '';
        updateHomePage();

        try {
            homeState.status = await fetchJson('/api/status', {
                headers:{ Accept:'application/json' }
            });
            homeState.statusLoaded = true;
        } catch (error) {
            homeState.status = null;
            homeState.statusLoaded = true;
            homeState.statusError = error.message || 'Unable to load server status.';
        } finally {
            homeState.statusLoading = false;
            updateHomePage();
        }

        if(canUseChat() && !homeState.sessionsLoaded){
            loadHomeSessions(true);
        }
    }

    async function loadHomeSessions(forceReloadMessages){
        if(!canUseChat() || homeState.isLoadingSessions){
            return;
        }

        homeState.isLoadingSessions = true;
        homeState.sessionsError = '';
        updateHomePage();

        try {
            var sessions = await fetchJson('/api/sessions', {
                headers:getHomeAuthHeaders()
            }, getTokenRecoveryMessage());
            if(!sessions){
                return;
            }

            homeState.sessions = sortSessions(Array.isArray(sessions) ? sessions : []);
            homeState.sessionsLoaded = true;

            if(homeState.selectedSessionId && !containsSession(homeState.selectedSessionId)){
                homeState.selectedSessionId = '';
            }

            if(!homeState.selectedSessionId && homeState.sessions.length > 0){
                homeState.selectedSessionId = homeState.sessions[0].id;
                storeSelectedSessionId(homeState.selectedSessionId);
            }

            updateHomePage();

            if(homeState.selectedSessionId && (forceReloadMessages || homeState.messagesLoadedFor !== homeState.selectedSessionId)){
                await loadMessages(homeState.selectedSessionId);
            }

            if(!homeState.selectedSessionId){
                homeState.messages = [];
                homeState.messagesLoadedFor = '';
                updateHomePage();
            }
        } catch (error) {
            homeState.sessions = [];
            homeState.messages = [];
            homeState.messagesLoadedFor = '';
            homeState.sessionsError = error.message || 'Unable to load chat sessions.';
            updateHomePage();
        } finally {
            homeState.isLoadingSessions = false;
            updateHomePage();
        }
    }

    function selectSession(sessionId){
        if(!sessionId){
            homeState.selectedSessionId = '';
            homeState.messages = [];
            homeState.messagesLoadedFor = '';
            storeSelectedSessionId('');
            updateHomePage();
            return;
        }

        if(homeState.selectedSessionId === sessionId && homeState.messagesLoadedFor === sessionId){
            updateHomePage();
            return;
        }

        homeState.selectedSessionId = sessionId;
        homeState.messages = [];
        homeState.messagesLoadedFor = '';
        storeSelectedSessionId(sessionId);
        updateHomePage();
        loadMessages(sessionId);
    }

    async function loadMessages(sessionId){
        if(!sessionId || !canUseChat()){
            return;
        }

        var requestId = ++homeState.requestCounter;
        homeState.isLoadingMessages = true;
        homeState.sessionsError = '';
        updateHomePage();

        try {
            var payload = await fetchJson(`/api/sessions/${encodeURIComponent(sessionId)}/messages?page=1&pageSize=100`, {
                headers:getHomeAuthHeaders()
            }, getTokenRecoveryMessage());
            if(!payload || requestId !== homeState.requestCounter){
                return;
            }

            var messages = Array.isArray(payload.messages) ? payload.messages : [];
            homeState.messages = sortMessages(messages);
            homeState.messagesLoadedFor = sessionId;
        } catch (error) {
            if(requestId !== homeState.requestCounter){
                return;
            }

            homeState.messages = [];
            homeState.messagesLoadedFor = '';
            homeState.sessionsError = error.message || 'Unable to load message history.';
        } finally {
            if(requestId === homeState.requestCounter){
                homeState.isLoadingMessages = false;
                updateHomePage();
                scrollChatToBottom();
            }
        }
    }

    async function createSession(){
        if(!canUseChat() || homeState.isCreatingSession){
            return;
        }

        homeState.isCreatingSession = true;
        homeState.sessionsError = '';
        updateHomePage();

        try {
            var session = await fetchJson('/api/sessions', {
                method:'POST',
                headers:getHomeJsonAuthHeaders(),
                body:JSON.stringify({ title:buildSessionTitle() })
            }, getTokenRecoveryMessage());
            if(!session){
                return;
            }

            homeState.sessions = sortSessions([session].concat(homeState.sessions.filter(function(existing){
                return existing.id !== session.id;
            })));
            homeState.sessionsLoaded = true;
            homeState.selectedSessionId = session.id;
            homeState.messages = [];
            homeState.messagesLoadedFor = session.id;
            storeSelectedSessionId(session.id);
        } catch (error) {
            homeState.sessionsError = error.message || 'Unable to create a new chat session.';
        } finally {
            homeState.isCreatingSession = false;
            updateHomePage();
            scrollChatToBottom();
        }
    }

    async function sendMessage(){
        var input = document.getElementById('chatInput');
        if(!input || homeState.isSending || !canUseChat() || !homeState.selectedSessionId){
            return;
        }

        var messageText = input.value.trim();
        if(!messageText){
            updateComposerState();
            return;
        }

        var userMessage = {
            id:'client-user-' + Date.now(),
            sessionId:homeState.selectedSessionId,
            role:'User',
            content:messageText,
            timestamp:new Date().toISOString(),
            usage:null
        };

        homeState.messages = sortMessages(homeState.messages.concat([userMessage]));
        input.value = '';
        homeState.isSending = true;
        homeState.sessionsError = '';
        updateHomePage();
        scrollChatToBottom();

        try {
            var assistantMessage = await fetchJson(`/api/sessions/${encodeURIComponent(homeState.selectedSessionId)}/messages`, {
                method:'POST',
                headers:getHomeJsonAuthHeaders(),
                body:JSON.stringify({ message:messageText })
            }, getTokenRecoveryMessage());
            if(!assistantMessage){
                return;
            }

            homeState.messages = sortMessages(homeState.messages.concat([assistantMessage]));
            homeState.messagesLoadedFor = homeState.selectedSessionId;
            updateSessionActivity(homeState.selectedSessionId, assistantMessage.timestamp);
        } catch (error) {
            homeState.messages = sortMessages(homeState.messages.concat([createClientSystemMessage(error.message || 'Unable to send the message right now.')]));
        } finally {
            homeState.isSending = false;
            updateHomePage();
            scrollChatToBottom();
        }
    }

    async function createBootstrapToken(){
        if(homeState.isBootstrapping){
            return;
        }

        homeState.isBootstrapping = true;
        homeState.authError = '';
        homeState.bootstrapMessage = '';
        updateHomePage();

        try {
            var response = await fetch('/api/tokens/bootstrap', {
                method:'POST',
                headers:{
                    Accept:'application/json',
                    'Content-Type':'application/json'
                },
                body:JSON.stringify({ name:'web-browser' })
            });
            var payload = await readJsonResponse(response);
            if(response.ok && payload && payload.token){
                homeState.token = payload.token;
                homeState.tokenDraft = payload.token;
                storeToken(payload.token);
                storeTokenId(payload.tokenId || '');
                window.location.reload();
                return;
            }

            if(response.status === 403){
                homeState.bootstrapMessage = 'Bootstrap token creation is only available before any tokens exist. Paste an existing token below or create one with Docker.';
                homeState.bootstrapMessageKind = 'info';
            } else {
                homeState.authError = getApiErrorMessage(response, payload, 'Unable to create a bootstrap token.');
            }
        } catch (error) {
            homeState.authError = error && error.message ? error.message : 'Unable to create a bootstrap token.';
        } finally {
            homeState.isBootstrapping = false;
            updateHomePage();
        }
    }

    // Home chat helpers
    function canUseChat(){
        return !!(homeState.statusLoaded && !homeState.statusError && homeState.status && homeState.status.setupComplete !== false && homeState.token);
    }

    function containsSession(sessionId){
        for(var i=0;i<homeState.sessions.length;i++){
            if(homeState.sessions[i].id === sessionId){
                return true;
            }
        }

        return false;
    }

    function getHomeStatusPillText(){
        if(homeState.statusLoading){
            return 'Checking status';
        }

        if(homeState.statusError){
            return 'Status error';
        }

        if(homeState.status && homeState.status.setupComplete === false){
            return 'Setup required';
        }

        if(!homeState.token){
            return 'Token required';
        }

        if(homeState.isBootstrapping){
            return 'Creating token';
        }

        if(homeState.isCreatingSession){
            return 'Creating session';
        }

        if(homeState.isSending){
            return 'Waiting for reply';
        }

        if(homeState.isLoadingMessages){
            return 'Loading messages';
        }

        return 'Chat ready';
    }

    function getHomeStatusText(){
        if(homeState.statusLoading){
            return 'Checking server status before enabling the chat experience.';
        }

        if(homeState.statusError){
            return 'The server status endpoint is currently unavailable.';
        }

        if(homeState.status && homeState.status.setupComplete === false){
            return 'Complete the setup flow before creating or viewing sessions.';
        }

        if(!homeState.token){
            return 'Create or provide a bearer token to load sessions and message history.';
        }

        if(homeState.isBootstrapping){
            return 'Creating a browser token for this WebUI session.';
        }

        if(homeState.isSending){
            return 'The assistant is generating a response for the active session.';
        }

        if(homeState.selectedSessionId){
            return 'Conversation history loads through the REST session endpoints.';
        }

        return 'Create or select a session to start chatting with your assistant.';
    }

    function getComposerHelperText(canCompose){
        if(homeState.sessionsError){
            return homeState.sessionsError;
        }

        if(!canUseChat()){
            return 'Save a bearer token to enable chat features in the WebUI.';
        }

        if(homeState.sessions.length === 0){
            return 'Create a session first. Enter sends a message; Shift+Enter inserts a new line.';
        }

        if(!canCompose){
            return 'Select a session to load history before sending a message.';
        }

        return 'Enter sends a message. Shift+Enter inserts a new line.';
    }

    function getHomeAuthHeaders(){
        var headers = { Accept:'application/json' };
        if(homeState.token){
            headers.Authorization = 'Bearer ' + homeState.token;
        }

        return headers;
    }

    function getHomeJsonAuthHeaders(){
        var headers = getHomeAuthHeaders();
        headers['Content-Type'] = 'application/json';
        return headers;
    }

    async function fetchJson(url, options, unauthorizedMessage){
        var response = await fetch(url, options || {});

        if(response.status === 401 || response.status === 403){
            handleUnauthorized(unauthorizedMessage || getTokenRecoveryMessage());
            return null;
        }

        if(!response.ok){
            var detail = await readErrorResponse(response);
            throw new Error(detail || ('Request failed with status ' + response.status + '.'));
        }

        if(response.status === 204){
            return null;
        }

        return response.json();
    }

    async function readErrorResponse(response){
        var text = '';
        try {
            text = await response.text();
        } catch (error) {
            return '';
        }

        if(!text){
            return '';
        }

        try {
            var payload = JSON.parse(text);
            return payload.detail || payload.title || payload.message || payload.error || text;
        } catch (error) {
            return text;
        }
    }

    function handleUnauthorized(message){
        homeState.token = '';
        homeState.tokenDraft = '';
        homeState.authError = message;
        homeState.bootstrapMessage = '';
        homeState.sessions = [];
        homeState.messages = [];
        homeState.sessionsLoaded = false;
        homeState.messagesLoadedFor = '';
        homeState.sessionsError = '';
        storeToken('');
        storeTokenId('');
        updateHomePage();
    }

    function clearToken(){
        homeState.token = '';
        homeState.tokenDraft = '';
        homeState.authError = '';
        homeState.bootstrapMessage = '';
        homeState.sessions = [];
        homeState.messages = [];
        homeState.sessionsLoaded = false;
        homeState.messagesLoadedFor = '';
        homeState.sessionsError = '';
        storeToken('');
        storeTokenId('');
        updateHomePage();
    }

    function storeToken(token){
        try {
            if(token){
                localStorage.setItem(storageKeys.token, token);
            } else {
                localStorage.removeItem(storageKeys.token);
            }
        } catch (error) {
            // Ignore storage errors so the SPA still works for the current page lifetime.
        }
    }

    function storeTokenId(tokenId){
        try {
            if(tokenId){
                localStorage.setItem(storageKeys.tokenId, tokenId);
            } else {
                localStorage.removeItem(storageKeys.tokenId);
            }
        } catch (error) {
            // Ignore storage errors so the SPA still works for the current page lifetime.
        }
    }

    function storeSelectedSessionId(sessionId){
        try {
            if(sessionId){
                localStorage.setItem(storageKeys.sessionId, sessionId);
            } else {
                localStorage.removeItem(storageKeys.sessionId);
            }
        } catch (error) {
            // Ignore storage errors so the SPA still works for the current page lifetime.
        }
    }

    function sortSessions(sessions){
        return sessions.slice().sort(function(left, right){
            return getTimestampValue(right && right.updatedAt) - getTimestampValue(left && left.updatedAt);
        });
    }

    function sortMessages(messages){
        return messages.slice().sort(function(left, right){
            return getTimestampValue(left && left.timestamp) - getTimestampValue(right && right.timestamp);
        });
    }

    function updateSessionActivity(sessionId, timestamp){
        for(var i=0;i<homeState.sessions.length;i++){
            if(homeState.sessions[i].id === sessionId){
                homeState.sessions[i].updatedAt = timestamp || new Date().toISOString();
                homeState.sessions[i].status = 'Active';
                break;
            }
        }

        homeState.sessions = sortSessions(homeState.sessions);
    }

    function buildSessionTitle(){
        return 'Chat ' + new Date().toLocaleString([], {
            month:'short',
            day:'numeric',
            hour:'2-digit',
            minute:'2-digit'
        });
    }

    function createClientSystemMessage(content){
        return {
            id:'client-system-' + Date.now(),
            sessionId:homeState.selectedSessionId,
            role:'System',
            content:content,
            timestamp:new Date().toISOString(),
            usage:null
        };
    }

    function getTimestampValue(value){
        var date = value ? new Date(value) : null;
        var ticks = date ? date.getTime() : NaN;
        return isNaN(ticks) ? 0 : ticks;
    }

    function formatTimestamp(value){
        if(!value){
            return 'Just now';
        }

        var date = new Date(value);
        if(isNaN(date.getTime())){
            return value;
        }

        var now = new Date();
        if(date.toDateString() === now.toDateString()){
            return date.toLocaleTimeString([], { hour:'2-digit', minute:'2-digit' });
        }

        return date.toLocaleString([], {
            month:'short',
            day:'numeric',
            hour:'2-digit',
            minute:'2-digit'
        });
    }

    function formatUsage(usage){
        if(!usage){
            return '';
        }

        var segments = [];
        if(typeof usage.inputTokens === 'number'){
            segments.push('In ' + usage.inputTokens);
        }
        if(typeof usage.outputTokens === 'number'){
            segments.push('Out ' + usage.outputTokens);
        }
        if(typeof usage.estimatedCostUsd === 'number'){
            segments.push('$' + usage.estimatedCostUsd.toFixed(4));
        }

        return segments.length ? 'Tokens · ' + segments.join(' · ') : '';
    }

    function formatSessionOption(session){
        var label = (session && session.title) ? session.title : 'Untitled session';
        if(session && session.status && session.status !== 'Active'){
            label += ' · ' + session.status;
        }

        if(session && session.updatedAt){
            label += ' · ' + formatTimestamp(session.updatedAt);
        }

        return label;
    }

    function normalizeRole(role){
        return ((role || 'Assistant') + '').toLowerCase();
    }

    function getRoleLabel(role){
        switch(role){
            case 'user': return 'You';
            case 'assistant': return 'Assistant';
            case 'system': return 'System';
            case 'tool': return 'Tool';
            default: return 'Assistant';
        }
    }

    function formatMessageContent(content){
        return escapeHtml(content || '').replace(/\n/g, '<br>');
    }

    function escapeHtml(value){
        return String(value == null ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function scrollChatToBottom(){
        var host = document.getElementById('chatMessages');
        if(!host){
            return;
        }

        window.requestAnimationFrame(function(){
            host.scrollTop = host.scrollHeight;
        });
    }

    function renderHomePage(){
        return `
<!-- Page: Home -->
<section class="page-shell">
    <header class="page-header">
        <div>
            <h1>Home</h1>
            <p>Resume saved assistant sessions, review recent messages, and send new prompts from the main OpenVEPA workspace.</p>
        </div>
        <span class="pill" id="homeStatusPill">Checking status</span>
    </header>

    <div class="home-layout">
        <section class="card chat-panel">
            <div class="section-heading">
                <div>
                    <h2>Chat messages</h2>
                    <p id="chatStatusText">Loading assistant status…</p>
                </div>
                <div class="loading-indicator" id="chatLoadingIndicator" hidden aria-hidden="true"><span></span><span></span><span></span></div>
            </div>
            <div class="chat-messages" id="chatMessages" aria-live="polite" aria-busy="false"></div>
        </section>

        <section class="card chat-composer">
            <div id="chatAuthPanel" class="auth-panel" hidden></div>

            <div id="chatComposerPanel" class="chat-composer-panel" hidden>
                <div class="section-heading">
                    <div>
                        <h2>Chat input</h2>
                        <p>Switch sessions, create a new conversation, and send messages with the configured API token.</p>
                    </div>
                    <button type="button" class="subtle-button" id="changeTokenButton">Change token</button>
                </div>

                <div class="chat-toolbar">
                    <label class="field" for="chatSessionSelect">
                        <span>Session</span>
                        <select id="chatSessionSelect" class="control session-select"></select>
                    </label>
                    <div class="toolbar-actions">
                        <button type="button" class="secondary-button" id="newSessionButton">+ New Session</button>
                    </div>
                </div>

                <div class="composer-row">
                    <label class="field" for="chatInput">
                        <span>Message</span>
                        <textarea id="chatInput" class="control chat-textarea" placeholder="Type a message to your assistant…"></textarea>
                    </label>
                    <button type="button" class="primary-button send-button" id="chatSendButton" aria-label="Send message">▶</button>
                </div>

                <div class="helper-row">
                    <p class="helper-note" id="chatHelperText">Enter sends a message. Shift+Enter inserts a new line.</p>
                </div>
            </div>
        </section>
    </div>
</section>`;
    }

    function renderPlaceholderPage(icon, title, description, detail, boxes){
        var metaHtml = '';
        for(var i=0;i<boxes.length;i++){
            metaHtml += `
<div class="meta-box">
    <h3>${boxes[i].title}</h3>
    <p>${boxes[i].text}</p>
</div>`;
        }

        return `
<!-- Page: ${title} -->
<section class="page-shell">
    <header class="page-header">
        <div>
            <h1>${title}</h1>
            <p>${description}</p>
        </div>
        <span class="pill">Coming soon</span>
    </header>

    <section class="card placeholder-card">
        <div class="section-heading">
            <div style="display:flex;align-items:center;gap:1rem;flex-wrap:wrap">
                <div class="placeholder-icon" aria-hidden="true">${icon}</div>
                <div class="placeholder-copy">
                    <h2>${title} page scaffold</h2>
                    <p>${detail}</p>
                </div>
            </div>
            <div class="loading-indicator" aria-hidden="true"><span></span><span></span><span></span></div>
        </div>
        <div class="placeholder-meta">${metaHtml}
        </div>
    </section>
</section>`;
    }

    function isMobile(){
        return mobileMedia.matches;
    }

    function syncSidebarState(){
        if(isMobile()){
            document.body.classList.remove('sidebar-collapsed');
            var mobileOpen = document.body.classList.contains('mobile-sidebar-open');
            sidebarOverlay.hidden = !mobileOpen;
            sidebarToggle.setAttribute('aria-expanded', mobileOpen ? 'true' : 'false');
            return;
        }

        document.body.classList.remove('mobile-sidebar-open');
        sidebarOverlay.hidden = true;
        sidebarToggle.setAttribute('aria-expanded', document.body.classList.contains('sidebar-collapsed') ? 'false' : 'true');
    }

    function closeMobileSidebar(){
        if(!isMobile()){
            return;
        }

        document.body.classList.remove('mobile-sidebar-open');
        syncSidebarState();
    }

    function toggleSidebar(){
        if(isMobile()){
            document.body.classList.toggle('mobile-sidebar-open');
        } else {
            document.body.classList.toggle('sidebar-collapsed');
        }

        syncSidebarState();
    }

    // Settings and catalog pages state.
    var llmProviders = [
        'ollama',
        'openai',
        'google',
        'anthropic',
        'mistral',
        'groq',
        'azure',
        'cohere',
        'together',
        'perplexity'
    ];
    var dashboardState = {
        llm:{
            config:null,
            usage:null,
            editingProvider:null,
            addingProvider:false,
            editDraft:null,
            addDraft:null,
            isLoading:false,
            isSaving:false,
            message:'',
            error:'',
            modelStatus:null,
            modelStatusLoading:false,
            addFetchStatus:null,
            addFetchedModels:null,
            editFetchStatus:null,
            editFetchedModels:null
        },
        agents:{
            items:null,
            detailByName:{},
            detailLoading:{},
            detailError:{},
            expandedName:'',
            isLoading:false,
            error:'',
            editingSystem:false,
            systemPrefs:{displayName:'',tone:'Professional'},
            systemPrefsSaving:false,
            systemPrefsMessage:'',
            systemPrefsError:'',
            configDetail:null,
            configBudget:null,
            configTab:'overview',
            configLoading:false,
            configSaving:false,
            configError:'',
            configMessage:'',
            orchConfig:null,
            orchLoading:false,
            orchSaving:false,
            orchError:'',
            orchMessage:''
        },
        skills:{
            items:null,
            detailByName:{},
            detailLoading:{},
            detailError:{},
            expandedName:'',
            isLoading:false,
            error:''
        },
        preferences:{
            groups:null,
            editingKey:'',
            isLoading:false,
            isMutating:false,
            message:'',
            error:'',
            assistantName:'VEPA'
        },
        tokens:{
            items:null,
            isLoading:false,
            isCreating:false,
            revokingId:'',
            createResult:null,
            revealTimeoutId:0,
            message:'',
            error:''
        },
        channels:{
            items:null,
            detail:null,
            detailId:'',
            isLoading:false,
            isSaving:false,
            message:'',
            error:''
        },
        timers:{
            tasks:[],
            loading:false,
            editingTask:null,
            showModal:false
        },
        restart:{
            isRestarting:false,
            polling:false,
            message:''
        },
        usage:{
            data:null,
            loading:false,
            error:'',
            period:'all',
            expandedAgents:{}
        }
    };

    // Shared helpers.
    function queuePageInit(callback){
        window.setTimeout(function(){
            if(typeof callback === 'function'){
                callback();
            }
        }, 0);
    }

    function getAuthToken(){
        try{
            return window.localStorage.getItem(storageKeys.token) || '';
        } catch(error){
            return '';
        }
    }

    function getStoredTokenId(){
        try{
            return window.localStorage.getItem(storageKeys.tokenId) || '';
        } catch(error){
            return '';
        }
    }

    function apiRequest(url, options){
        var requestOptions = options || {};
        var hasBody = Object.prototype.hasOwnProperty.call(requestOptions, 'body') && requestOptions.body !== undefined && requestOptions.body !== null;
        var headers = { Accept:'application/json' };
        var token = getAuthToken();
        if(token){
            headers.Authorization = 'Bearer ' + token;
        }
        if(hasBody){
            headers['Content-Type'] = 'application/json';
        }
        if(requestOptions.headers){
            for(var headerName in requestOptions.headers){
                if(Object.prototype.hasOwnProperty.call(requestOptions.headers, headerName)){
                    headers[headerName] = requestOptions.headers[headerName];
                }
            }
        }

        return fetch(url, {
            method:requestOptions.method || 'GET',
            headers:headers,
            body:hasBody ? requestOptions.body : undefined
        }).then(function(response){
            return response.text().then(function(text){
                var data = null;
                if(text){
                    try{
                        data = JSON.parse(text);
                    } catch(parseError){
                        data = text;
                    }
                }

                if(!response.ok){
                    var message = 'Request failed with status ' + response.status + '.';
                    if(response.status === 401 || response.status === 403){
                        storeToken('');
                        storeTokenId('');
                        message = getTokenRecoveryMessage();
                    } else if(data && typeof data === 'object' && data.error){
                        message = data.error;
                    } else if(typeof data === 'string' && data){
                        message = data;
                    }

                    throw new Error(message);
                }

                return data;
            });
        });
    }

    function escapeHtml(value){
        return String(value == null ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function formatNumber(value){
        var number = Number(value);
        return isFinite(number) ? number.toLocaleString() : '0';
    }

    function formatCurrency(value){
        var number = Number(value);
        if(!isFinite(number)){
            number = 0;
        }

        var fractionDigits = number >= 10 ? 2 : 4;
        return '$' + number.toFixed(fractionDigits);
    }

    function formatConfidence(value){
        var number = Number(value);
        return isFinite(number) ? Math.round(number * 100) + '%' : '—';
    }

    function formatUpdatedAt(value){
        if(!value){
            return 'Unknown';
        }

        var timestamp = new Date(value);
        return isNaN(timestamp.getTime()) ? String(value) : timestamp.toLocaleString();
    }

    function truncateText(value, maxLength){
        var text = String(value == null ? '' : value);
        return text.length > maxLength ? text.slice(0, maxLength) + '\n…' : text;
    }

    function pluralize(count, singular, plural){
        return count === 1 ? singular : (plural || singular + 's');
    }

    function getTokenRecoveryMessage(){
        return 'Token missing or expired.';
    }

    function isTokenRecoveryMessage(message){
        var normalized = String(message || '').toLowerCase();
        return normalized === getTokenRecoveryMessage().toLowerCase()
            || normalized.indexOf('sign in again') !== -1
            || normalized.indexOf('token missing') !== -1
            || normalized.indexOf('session has expired') !== -1;
    }

    function renderMessageWithLinks(message){
        if(isTokenRecoveryMessage(message)){
            return `Token missing or expired. <a class='text-link' href='#/home'>Enter token</a> or <a class='text-link' href='#/user-settings/tokens'>manage tokens</a>.`;
        }

        return escapeHtml(message);
    }

    function renderErrorBanner(message){
        if(!message){
            return '';
        }

        return `<div class='error-banner'>${renderMessageWithLinks(message)}</div>`;
    }

    function renderStatusBanner(message, kind){
        if(!message){
            return '';
        }

        return `<div class='status-banner is-${kind || 'info'}'>${renderMessageWithLinks(message)}</div>`;
    }

    function renderLoadingPanel(message){
        return `
<div class='loading-panel'>
    <div class='loading-indicator' aria-hidden='true'><span></span><span></span><span></span></div>
    <p>${escapeHtml(message)}</p>
</div>`;
    }

    function renderSettingsEmptyState(title, description, actionLabel, actionAttribute){
        var actionHtml = actionLabel && actionAttribute
            ? `<button type='button' class='secondary-button' ${actionAttribute}>${escapeHtml(actionLabel)}</button>`
            : '';
        return `
<div class='empty-state'>
    <strong>${escapeHtml(title)}</strong>
    <p>${escapeHtml(description)}</p>
    ${actionHtml}
</div>`;
    }

    function renderTabBar(tabs, activeTab){
        var html = '<nav class="tab-bar">';
        for(var i=0;i<tabs.length;i++){
            var tab = tabs[i];
            var isActive = tab.id === activeTab;
            html += '<a href="' + escapeHtml(tab.href) + '" class="tab' + (isActive ? ' active' : '') + '">' + tab.icon + ' ' + escapeHtml(tab.label) + '</a>';
        }
        html += '</nav>';
        return html;
    }

    function renderCountBadge(count, singular, plural){
        return `<span class='badge is-muted'>${formatNumber(count)} ${escapeHtml(pluralize(count, singular, plural))}</span>`;
    }

    function renderAutonomyBadge(level){
        var normalized = Math.max(0, Math.min(4, Number(level) || 0));
        var stars = '';
        for(var i=0;i<4;i++){
            stars += `<span class='autonomy-star ${i < normalized ? 'is-filled' : ''}'>★</span>`;
        }

        return `<span class='badge is-accent'><span class='autonomy-stars' aria-hidden='true'>${stars}</span><span>Autonomy ${normalized}/4</span></span>`;
    }

    function getSkillTypeLabel(type){
        switch(Number(type)){
            case 0:
                return 'Native';
            case 1:
                return 'MCP';
            case 2:
                return 'Hybrid';
            default:
                return 'Unknown';
        }
    }

    function getPreferenceSourceLabel(source){
        switch(Number(source)){
            case 0:
                return 'Explicit';
            case 1:
                return 'Inferred';
            case 2:
                return 'Confirmed';
            default:
                return 'Unknown';
        }
    }

    function renderTokens(values, emptyText){
        if(!values || !values.length){
            return `<p class='field-hint'>${escapeHtml(emptyText)}</p>`;
        }

        var html = "<ul class='token-list'>";
        for(var i=0;i<values.length;i++){
            html += `<li class='token'>${escapeHtml(values[i])}</li>`;
        }
        html += '</ul>';
        return html;
    }

    function findPreferenceEntry(groups, key){
        if(!groups || !key){
            return null;
        }

        for(var category in groups){
            if(!Object.prototype.hasOwnProperty.call(groups, category)){
                continue;
            }

            var categoryEntries = groups[category];
            if(categoryEntries && Object.prototype.hasOwnProperty.call(categoryEntries, key)){
                return categoryEntries[key];
            }
        }

        return null;
    }

    function normalizePreferencesPayload(payload){
        var grouped = {};
        if(!payload){
            return grouped;
        }

        if(payload.entries && typeof payload.entries === 'object'){
            for(var entryKey in payload.entries){
                if(!Object.prototype.hasOwnProperty.call(payload.entries, entryKey)){
                    continue;
                }

                var flatEntry = payload.entries[entryKey] || {};
                var flatCategory = flatEntry.category || 'general';
                if(!grouped[flatCategory]){
                    grouped[flatCategory] = {};
                }
                grouped[flatCategory][flatEntry.key || entryKey] = flatEntry;
            }

            return grouped;
        }

        for(var categoryKey in payload){
            if(!Object.prototype.hasOwnProperty.call(payload, categoryKey)){
                continue;
            }

            var entries = payload[categoryKey];
            if(!entries || typeof entries !== 'object'){
                continue;
            }

            grouped[categoryKey] = {};
            for(var prefKey in entries){
                if(!Object.prototype.hasOwnProperty.call(entries, prefKey)){
                    continue;
                }

                var entry = entries[prefKey] || {};
                grouped[categoryKey][entry.key || prefKey] = entry;
            }
        }

        return grouped;
    }

    function upsertPreference(key, value, category){
        var existing = findPreferenceEntry(dashboardState.preferences.groups, key);
        var url = existing ? '/api/preferences/' + encodeURIComponent(key) : '/api/preferences';
        var method = existing ? 'PUT' : 'POST';
        var payload = existing
            ? { value:value, category:category }
            : { key:key, value:value, category:category };

        return apiRequest(url, {
            method:method,
            body:JSON.stringify(payload)
        });
    }

    function deletePreference(key){
        return apiRequest('/api/preferences/' + encodeURIComponent(key), {
            method:'DELETE'
        });
    }

    // LLM settings page.
    function renderLlmSettingsPage(){
        queuePageInit(initLlmSettingsPage);
        return `
<!-- Page: LLM Settings -->
<section class='page-shell'>
    <header class='page-header'>
        <div>
            <h1>LLM Settings</h1>
            <p>Review the current provider configuration and update provider defaults.</p>
        </div>
        <span class='pill'>Provider configuration</span>
    </header>
    <div class='settings-grid'>
        <section class='card settings-card' id='llmConfigCard'>${renderLoadingPanel('Loading LLM configuration…')}</section>
        <section class='card settings-card' id='llmUsageCard'>
            <div class='section-heading'>
                <div>
                    <h2>Usage statistics</h2>
                    <p>Detailed usage tracking has moved to its own page.</p>
                </div>
            </div>
            <a href='#/usage' class='primary-button' style='display:inline-block;text-decoration:none;margin-top:0.5rem'>View detailed usage &#8594;</a>
        </section>
    </div>
</section>`;
    }

    function initLlmSettingsPage(){
        renderLlmSettingsPanels();
        if(!dashboardState.llm.config){
            loadLlmSettings(false);
        }
    }

    function loadLlmSettings(force){
        var state = dashboardState.llm;
        if(state.isLoading){
            return Promise.resolve();
        }
        if(!force && state.config){
            renderLlmSettingsPanels();
            return Promise.resolve();
        }

        state.isLoading = true;
        state.error = '';
        renderLlmSettingsPanels();

        return apiRequest('/api/system/llm-config').then(function(result){
            state.config = result;
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isLoading = false;
            renderLlmSettingsPanels();
        });
    }

    function renderLlmProviderOptions(selectedType){
        var html = '';
        for(var i=0;i<llmProviders.length;i++){
            var provider = llmProviders[i];
            html += '<option value="'+escapeHtml(provider)+'"'+(provider === selectedType ? ' selected' : '')+'>'+escapeHtml(provider)+'</option>';
        }
        return html;
    }

    function getConfiguredProviderNames(){
        var config = dashboardState.llm.config;
        if(!config || !config.providers){
            return [];
        }
        return config.providers.map(function(p){ return p.name; });
    }

    function renderLlmConfigCard(){
        var state = dashboardState.llm;
        var config = state.config;
        if(state.isLoading && !config){
            return renderLoadingPanel('Loading LLM configuration…');
        }
        if(!config){
            return renderStatusBanner(state.error, 'error') + renderSettingsEmptyState(
                'LLM configuration unavailable',
                'We could not load the current provider configuration.',
                'Retry',
                "data-llm-action='refresh'");
        }

        var banner = renderStatusBanner(state.error, 'error') || renderStatusBanner(state.message, 'success');
        var providers = config.providers || [];
        var defaultProvider = config.defaultProvider || 'unknown';
        var html = '';

        html += '<div class="section-heading">';
        html += '<div>';
        html += '<h2>Configured providers</h2>';
        html += '<p>All configured LLM providers. The default provider is highlighted.</p>';
        html += '</div>';
        html += '<div class="settings-actions">';
        html += '<button type="button" class="secondary-button" id="llmRefreshButton"' + (state.isSaving ? ' disabled' : '') + '>Refresh</button>';
        html += '<button type="button" class="primary-button" id="llmAddProviderButton"' + (state.isSaving ? ' disabled' : '') + '>Add Provider</button>';
        html += '</div>';
        html += '</div>';
        html += '<div id="llmModelStatusArea">' + renderModelStatusIndicator() + '</div>';
        html += '<div style="margin-bottom:0.75rem"><button type="button" class="secondary-button" id="llmTestConnectionButton"' + (state.modelStatusLoading || state.isSaving ? ' disabled' : '') + '>' + (state.modelStatusLoading ? 'Testing…' : 'Test Connection') + '</button></div>';
        html += banner;

        if(state.addingProvider){
            var draft = state.addDraft || {type:llmProviders[0],displayName:'',modelId:'',endpoint:'',apiKey:''};
            html += '<div class="provider-card" style="border-color:var(--accent)">';
            html += '<div class="section-heading"><div><h2>Add new provider</h2></div></div>';
            html += '<div class="field">';
            html += '<span>Provider Type</span>';
            html += '<select id="llmNewProviderSelect" class="control"' + (state.isSaving ? ' disabled' : '') + '>' + renderLlmProviderOptions(draft.type) + '</select>';
            html += '</div>';
            html += '<div class="field">';
            html += '<span>Display Name</span>';
            html += '<input id="llmNewDisplayNameInput" class="control" type="text" value="'+escapeHtml(draft.displayName)+'" placeholder="e.g. Ollama Home"' + (state.isSaving ? ' disabled' : '') + '>';
            html += '</div>';
            html += '<div class="field">';
            html += '<span>Model</span>';
            html += '<div style="display:flex;gap:0.5rem;align-items:flex-start">';
            if(state.addFetchedModels && state.addFetchedModels.length > 0){
                html += '<select id="llmNewModelSelect" class="control" style="flex:1;margin-bottom:0"' + (state.isSaving ? ' disabled' : '') + '>';
                for(var mi=0;mi<state.addFetchedModels.length;mi++){
                    var m = state.addFetchedModels[mi];
                    html += '<option value="'+escapeHtml(m)+'"' + (m === draft.modelId ? ' selected' : '') + '>' + escapeHtml(m) + '</option>';
                }
                html += '<option value="__custom__">\u2014 Custom model \u2014</option>';
                html += '</select>';
            } else {
                html += '<input id="llmNewModelInput" class="control" style="flex:1;margin-bottom:0" type="text" value="'+escapeHtml(draft.modelId)+'" placeholder="model-id"' + (state.isSaving ? ' disabled' : '') + '>';
            }
            html += '<button type="button" class="secondary-button" id="llmNewFetchModelsBtn" style="white-space:nowrap"' + (state.isSaving || (state.addFetchStatus && state.addFetchStatus.type === 'loading') ? ' disabled' : '') + '>' + (state.addFetchStatus && state.addFetchStatus.type === 'loading' ? 'Fetching\u2026' : 'Fetch Models') + '</button>';
            html += '</div>';
            if(state.addFetchedModels && state.addFetchedModels.length > 0){
                html += '<input id="llmNewModelCustom" class="control hidden" type="text" value="'+escapeHtml(draft.modelId)+'" placeholder="Enter custom model name" style="margin-top:0.5rem">';
            }
            html += renderProviderFetchStatus(state.addFetchStatus);
            html += '</div>';
            html += '<div class="field">';
            html += '<span>Endpoint</span>';
            html += '<input id="llmNewEndpointInput" class="control" type="url" value="'+escapeHtml(draft.endpoint)+'" placeholder="https://api.example.com/v1"' + (state.isSaving ? ' disabled' : '') + '>';
            html += '</div>';
            html += '<div class="field">';
            html += '<span>API Key <span style="font-weight:normal;opacity:0.7">(optional)</span></span>';
            html += '<input id="llmNewApiKeyInput" class="control" type="password" value="'+escapeHtml(draft.apiKey)+'" placeholder="sk-..."' + (state.isSaving ? ' disabled' : '') + '>';
            html += '</div>';
            html += '<div class="provider-actions">';
            html += '<button type="button" class="secondary-button" id="llmCancelAddButton"' + (state.isSaving ? ' disabled' : '') + '>Cancel</button>';
            html += '<button type="button" class="primary-button" id="llmSaveAddButton"' + (state.isSaving ? ' disabled' : '') + '>' + (state.isSaving ? 'Saving\u2026' : 'Add') + '</button>';
            html += '</div>';
            html += '</div>';
        }

        html += '<div class="provider-grid">';
        for(var i=0;i<providers.length;i++){
            var p = providers[i];
            var isDefault = p.name === defaultProvider;
            var isEditing = state.editingProvider === p.name;

            html += '<div class="provider-card' + (isDefault ? ' provider-default' : '') + '">';
            html += '<div class="provider-header">';
            html += '<span class="provider-name">' + escapeHtml(p.displayName || p.name) + '</span>';
            html += '<span class="pill" style="background:var(--surface-raised);color:var(--text-secondary);font-size:0.78em">' + escapeHtml(p.type || p.name) + '</span>';
            if(isDefault){
                html += '<span class="pill">Default</span>';
            }
            html += '</div>';

            if(isEditing){
                var draft = state.editDraft || {displayName:p.displayName||'',modelId:p.modelId||'',endpoint:p.endpoint||'',apiKey:''};
                html += '<div class="field">';
                html += '<span>Display Name</span>';
                html += '<input class="control llm-edit-displayname" type="text" value="'+escapeHtml(draft.displayName)+'" data-provider="'+escapeHtml(p.name)+'"' + (state.isSaving ? ' disabled' : '') + '>';
                html += '</div>';
                html += '<div class="field">';
                html += '<span>Model</span>';
                html += '<div style="display:flex;gap:0.5rem;align-items:flex-start">';
                if(state.editFetchedModels && state.editFetchedModels.length > 0){
                    html += '<select class="control llm-edit-model-select" style="flex:1;margin-bottom:0" data-provider="'+escapeHtml(p.name)+'"' + (state.isSaving ? ' disabled' : '') + '>';
                    for(var emi=0;emi<state.editFetchedModels.length;emi++){
                        var em = state.editFetchedModels[emi];
                        html += '<option value="'+escapeHtml(em)+'"' + (em === draft.modelId ? ' selected' : '') + '>' + escapeHtml(em) + '</option>';
                    }
                    html += '<option value="__custom__">\u2014 Custom model \u2014</option>';
                    html += '</select>';
                } else {
                    html += '<input class="control llm-edit-model" style="flex:1;margin-bottom:0" type="text" value="'+escapeHtml(draft.modelId)+'" data-provider="'+escapeHtml(p.name)+'"' + (state.isSaving ? ' disabled' : '') + '>';
                }
                html += '<button type="button" class="secondary-button llm-edit-fetch-btn" style="white-space:nowrap" data-provider="'+escapeHtml(p.name)+'"' + (state.isSaving || (state.editFetchStatus && state.editFetchStatus.type === 'loading') ? ' disabled' : '') + '>' + (state.editFetchStatus && state.editFetchStatus.type === 'loading' ? 'Fetching\u2026' : 'Fetch Models') + '</button>';
                html += '</div>';
                if(state.editFetchedModels && state.editFetchedModels.length > 0){
                    html += '<input class="control llm-edit-model-custom hidden" type="text" value="'+escapeHtml(draft.modelId)+'" data-provider="'+escapeHtml(p.name)+'" placeholder="Enter custom model name" style="margin-top:0.5rem">';
                }
                html += renderProviderFetchStatus(state.editFetchStatus);
                html += '</div>';
                html += '<div class="field">';
                html += '<span>Endpoint</span>';
                html += '<input class="control llm-edit-endpoint" type="url" value="'+escapeHtml(draft.endpoint)+'" data-provider="'+escapeHtml(p.name)+'"' + (state.isSaving ? ' disabled' : '') + '>';
                html += '</div>';
                html += '<div class="field">';
                html += '<span>API Key <span style="font-weight:normal;opacity:0.7">(optional)</span></span>';
                html += '<input class="control llm-edit-apikey" type="password" value="'+escapeHtml(draft.apiKey)+'" data-provider="'+escapeHtml(p.name)+'"' + (state.isSaving ? ' disabled' : '') + '>';
                html += '</div>';
                html += '<div class="provider-actions">';
                html += '<button type="button" class="secondary-button llm-cancel-edit"' + (state.isSaving ? ' disabled' : '') + '>Cancel</button>';
                html += '<button type="button" class="primary-button llm-save-edit" data-provider="'+escapeHtml(p.name)+'"' + (state.isSaving ? ' disabled' : '') + '>' + (state.isSaving ? 'Saving\u2026' : 'Save') + '</button>';
                html += '</div>';
            } else {
                html += '<div class="provider-meta">';
                html += '<div class="provider-meta-row"><span class="provider-meta-label">Instance</span><span class="provider-meta-value" style="opacity:0.7;font-size:0.9em">' + escapeHtml(p.name) + '</span></div>';
                html += '<div class="provider-meta-row"><span class="provider-meta-label">Model</span><span class="provider-meta-value">' + escapeHtml(p.modelId || 'unknown') + '</span></div>';
                html += '<div class="provider-meta-row"><span class="provider-meta-label">Endpoint</span><span class="provider-meta-value">' + escapeHtml(p.endpoint || 'Not configured') + '</span></div>';
                html += '</div>';
                html += '<div class="provider-actions">';
                if(!isDefault){
                    html += '<button type="button" class="secondary-button llm-set-default" data-provider="'+escapeHtml(p.name)+'"' + (state.isSaving ? ' disabled' : '') + '>Set as default</button>';
                }
                html += '<button type="button" class="secondary-button llm-edit-provider" data-provider="'+escapeHtml(p.name)+'"' + (state.isSaving ? ' disabled' : '') + '>Edit</button>';
                if(providers.length > 1){
                    html += '<button type="button" class="secondary-button llm-remove-provider" data-provider="'+escapeHtml(p.name)+'"' + (state.isSaving ? ' disabled' : '') + '>Remove</button>';
                }
                html += '</div>';
            }
            html += '</div>';
        }
        html += '</div>';

        if(providers.length === 0){
            html += '<p class="helper-text">No providers configured. Use the Add Provider button to get started.</p>';
        } else {
            html += '<p class="helper-text">Only non-sensitive configuration values are displayed. API keys remain hidden from the WebUI.</p>';
        }

        return html;
    }

    function renderLlmUsageCard(){
        var state = dashboardState.llm;
        var usage = state.usage;
        if(state.isLoading && !usage){
            return renderLoadingPanel('Loading usage statistics…');
        }
        if(!usage){
            return renderStatusBanner(state.error, 'error') + renderSettingsEmptyState(
                'Usage data unavailable',
                'No usage summary could be loaded from the server.',
                'Retry',
                "data-llm-action='refresh'");
        }

        var html = `
<div class='section-heading'>
    <div>
        <h2>Usage summary</h2>
        <p>Totals are aggregated from the LLM audit log across all recorded requests.</p>
    </div>
    <div class='settings-actions'>
        <button type='button' class='secondary-button' id='llmUsageRefreshButton'>Refresh</button>
    </div>
</div>
<div class='stats-grid'>
    <div class='stat-card'>
        <div class='stat-label'>Call count</div>
        <div class='stat-value'>${formatNumber(usage.callCount)}</div>
    </div>
    <div class='stat-card'>
        <div class='stat-label'>Input tokens</div>
        <div class='stat-value'>${formatNumber(usage.inputTokens)}</div>
    </div>
    <div class='stat-card'>
        <div class='stat-label'>Output tokens</div>
        <div class='stat-value'>${formatNumber(usage.outputTokens)}</div>
    </div>
    <div class='stat-card'>
        <div class='stat-label'>Total tokens</div>
        <div class='stat-value'>${formatNumber(usage.totalTokens)}</div>
    </div>
    <div class='stat-card'>
        <div class='stat-label'>Estimated cost</div>
        <div class='stat-value'>${formatCurrency(usage.totalCostUsd)}</div>
    </div>
</div>`;

        // Time period breakdown
        if(usage.last24Hours || usage.last7Days || usage.last30Days){
            html += `
<h3 style='margin:1.25rem 0 0.5rem'>Activity by period</h3>
<div class='stats-grid'>`;
            if(usage.last24Hours){
                html += `
    <div class='stat-card'>
        <div class='stat-label'>Last 24 hours</div>
        <div class='stat-value'>${formatNumber(usage.last24Hours.calls)} calls</div>
        <div class='stat-sub'>${formatCurrency(usage.last24Hours.costUsd)}</div>
    </div>`;
            }
            if(usage.last7Days){
                html += `
    <div class='stat-card'>
        <div class='stat-label'>Last 7 days</div>
        <div class='stat-value'>${formatNumber(usage.last7Days.calls)} calls</div>
        <div class='stat-sub'>${formatCurrency(usage.last7Days.costUsd)}</div>
    </div>`;
            }
            if(usage.last30Days){
                html += `
    <div class='stat-card'>
        <div class='stat-label'>Last 30 days</div>
        <div class='stat-value'>${formatNumber(usage.last30Days.calls)} calls</div>
        <div class='stat-sub'>${formatCurrency(usage.last30Days.costUsd)}</div>
    </div>`;
            }
            html += '</div>';
        }

        // Provider/model breakdown table
        if(usage.byProvider && usage.byProvider.length > 0){
            html += `
<h3 style='margin:1.25rem 0 0.5rem'>Breakdown by provider / model</h3>
<div style='overflow-x:auto'>
<table class='data-table' style='width:100%;border-collapse:collapse;font-size:0.92em'>
<thead><tr>
    <th style='text-align:left;padding:0.4rem 0.6rem'>Provider</th>
    <th style='text-align:left;padding:0.4rem 0.6rem'>Model</th>
    <th style='text-align:right;padding:0.4rem 0.6rem'>Calls</th>
    <th style='text-align:right;padding:0.4rem 0.6rem'>Input tokens</th>
    <th style='text-align:right;padding:0.4rem 0.6rem'>Output tokens</th>
    <th style='text-align:right;padding:0.4rem 0.6rem'>Cost</th>
    <th style='text-align:left;padding:0.4rem 0.6rem;width:30%'></th>
</tr></thead><tbody>`;

            var maxCalls = 0;
            for(var i=0;i<usage.byProvider.length;i++){
                if(usage.byProvider[i].calls > maxCalls) maxCalls = usage.byProvider[i].calls;
            }

            for(var i=0;i<usage.byProvider.length;i++){
                var row = usage.byProvider[i];
                var pct = maxCalls > 0 ? Math.round((row.calls / maxCalls) * 100) : 0;
                html += '<tr>';
                html += '<td style="padding:0.4rem 0.6rem">' + escapeHtml(row.provider) + '</td>';
                html += '<td style="padding:0.4rem 0.6rem"><code>' + escapeHtml(row.model) + '</code></td>';
                html += '<td style="text-align:right;padding:0.4rem 0.6rem">' + formatNumber(row.calls) + '</td>';
                html += '<td style="text-align:right;padding:0.4rem 0.6rem">' + formatNumber(row.inputTokens) + '</td>';
                html += '<td style="text-align:right;padding:0.4rem 0.6rem">' + formatNumber(row.outputTokens) + '</td>';
                html += '<td style="text-align:right;padding:0.4rem 0.6rem">' + formatCurrency(row.estimatedCostUsd) + '</td>';
                html += '<td style="padding:0.4rem 0.6rem"><div style="background:var(--bg-tertiary);border-radius:4px;height:8px;overflow:hidden"><div style="background:var(--accent);height:100%;width:' + pct + '%"></div></div></td>';
                html += '</tr>';
            }
            html += '</tbody></table></div>';
        }

        html += '<p class="helper-text">Cost estimates are approximate and based on published provider pricing.</p>';
        return html;
    }

    function renderLlmSettingsPanels(){
        var configCard = document.getElementById('llmConfigCard');
        if(!configCard){
            return;
        }

        configCard.innerHTML = renderLlmConfigCard();

        var refreshButton = document.getElementById('llmRefreshButton');
        if(refreshButton){
            refreshButton.onclick = function(){ loadLlmSettings(true); };
        }
        var addProviderButton = document.getElementById('llmAddProviderButton');
        if(addProviderButton){
            addProviderButton.onclick = function(){
                dashboardState.llm.addingProvider = true;
                dashboardState.llm.addDraft = {type:llmProviders[0],displayName:'',modelId:'',endpoint:'',apiKey:''};
                dashboardState.llm.error = '';
                renderLlmSettingsPanels();
            };
        }
        var cancelAddButton = document.getElementById('llmCancelAddButton');
        if(cancelAddButton){
            cancelAddButton.onclick = function(){
                dashboardState.llm.addingProvider = false;
                dashboardState.llm.addDraft = null;
                dashboardState.llm.addFetchStatus = null;
                dashboardState.llm.addFetchedModels = null;
                dashboardState.llm.error = '';
                renderLlmSettingsPanels();
            };
        }
        var saveAddButton = document.getElementById('llmSaveAddButton');
        if(saveAddButton){
            saveAddButton.onclick = saveNewProvider;
        }
        var newFetchBtn = document.getElementById('llmNewFetchModelsBtn');
        if(newFetchBtn){
            newFetchBtn.onclick = function(){
                var sel = document.getElementById('llmNewProviderSelect');
                var epInput = document.getElementById('llmNewEndpointInput');
                var akInput = document.getElementById('llmNewApiKeyInput');
                if(sel && epInput){
                    fetchProviderModels('add', sel.value, epInput.value.trim(), akInput ? akInput.value.trim() : '');
                }
            };
        }
        var newModelSelect = document.getElementById('llmNewModelSelect');
        if(newModelSelect){
            newModelSelect.onchange = function(){
                var customInput = document.getElementById('llmNewModelCustom');
                if(this.value === '__custom__'){
                    if(customInput) customInput.classList.remove('hidden');
                } else {
                    if(customInput) customInput.classList.add('hidden');
                    if(dashboardState.llm.addDraft) dashboardState.llm.addDraft.modelId = this.value;
                }
            };
        }

        var editButtons = document.querySelectorAll('.llm-edit-provider');
        for(var i=0;i<editButtons.length;i++){
            editButtons[i].onclick = function(){
                var providerName = this.getAttribute('data-provider');
                var config = dashboardState.llm.config;
                var existing = (config && config.providers || []).filter(function(p){ return p.name === providerName; })[0];
                dashboardState.llm.editingProvider = providerName;
                dashboardState.llm.editDraft = {displayName:(existing && existing.displayName)||'',modelId:(existing && existing.modelId)||'',endpoint:(existing && existing.endpoint)||'',apiKey:''};
                dashboardState.llm.error = '';
                renderLlmSettingsPanels();
            };
        }
        var cancelEditButtons = document.querySelectorAll('.llm-cancel-edit');
        for(var ci=0;ci<cancelEditButtons.length;ci++){
            cancelEditButtons[ci].onclick = function(){
                dashboardState.llm.editingProvider = null;
                dashboardState.llm.editDraft = null;
                dashboardState.llm.editFetchStatus = null;
                dashboardState.llm.editFetchedModels = null;
                dashboardState.llm.error = '';
                renderLlmSettingsPanels();
            };
        }
        var saveEditButtons = document.querySelectorAll('.llm-save-edit');
        for(var si=0;si<saveEditButtons.length;si++){
            saveEditButtons[si].onclick = function(){
                saveEditedProvider(this.getAttribute('data-provider'));
            };
        }
        var editFetchButtons = document.querySelectorAll('.llm-edit-fetch-btn');
        for(var efi=0;efi<editFetchButtons.length;efi++){
            editFetchButtons[efi].onclick = function(){
                var pn = this.getAttribute('data-provider');
                var epInput = document.querySelector('.llm-edit-endpoint[data-provider="'+pn+'"]');
                if(epInput){
                    fetchProviderModels('edit', pn, epInput.value.trim());
                }
            };
        }
        var editModelSelects = document.querySelectorAll('.llm-edit-model-select');
        for(var emsi=0;emsi<editModelSelects.length;emsi++){
            editModelSelects[emsi].onchange = function(){
                var pn = this.getAttribute('data-provider');
                var customInput = document.querySelector('.llm-edit-model-custom[data-provider="'+pn+'"]');
                if(this.value === '__custom__'){
                    if(customInput) customInput.classList.remove('hidden');
                } else {
                    if(customInput) customInput.classList.add('hidden');
                    if(dashboardState.llm.editDraft) dashboardState.llm.editDraft.modelId = this.value;
                }
            };
        }
        var setDefaultButtons = document.querySelectorAll('.llm-set-default');
        for(var di=0;di<setDefaultButtons.length;di++){
            setDefaultButtons[di].onclick = function(){
                setDefaultProvider(this.getAttribute('data-provider'));
            };
        }
        var removeButtons = document.querySelectorAll('.llm-remove-provider');
        for(var ri=0;ri<removeButtons.length;ri++){
            removeButtons[ri].onclick = function(){
                removeProvider(this.getAttribute('data-provider'));
            };
        }

        var retryButtons = document.querySelectorAll('[data-llm-action="refresh"]');
        for(var ti=0;ti<retryButtons.length;ti++){
            retryButtons[ti].onclick = function(){ loadLlmSettings(true); };
        }

        var testConnectionButton = document.getElementById('llmTestConnectionButton');
        if(testConnectionButton){
            testConnectionButton.onclick = function(){ testLlmConnection(); };
        }
    }

    function testLlmConnection(providerOverride, endpointOverride){
        var state = dashboardState.llm;
        state.modelStatusLoading = true;
        state.modelStatus = null;
        renderLlmSettingsPanels();

        var queryParams = [];
        if(providerOverride){
            queryParams.push('provider=' + encodeURIComponent(providerOverride));
        }
        if(endpointOverride){
            queryParams.push('endpoint=' + encodeURIComponent(endpointOverride));
        }
        var url = '/api/system/models' + (queryParams.length ? '?' + queryParams.join('&') : '');

        apiRequest(url).then(function(result){
            state.modelStatus = result;
        }).catch(function(error){
            state.modelStatus = { success:false, models:[], provider:'unknown', endpoint:'', error:error.message, diagnostics:'Request failed: ' + error.message };
        }).finally(function(){
            state.modelStatusLoading = false;
            renderLlmSettingsPanels();
        });
    }

    function renderModelStatusIndicator(){
        var state = dashboardState.llm;
        if(state.modelStatusLoading){
            return '<div class="model-status-indicator model-status-loading"><span class="loading-indicator" aria-hidden="true"><span></span><span></span><span></span></span> <span>Querying models…</span></div>';
        }
        if(!state.modelStatus){
            return '';
        }
        var ms = state.modelStatus;
        if(ms.success){
            return '<div class="model-status-indicator model-status-success">✅ <strong>Connected</strong> — ' + escapeHtml(ms.diagnostics || (ms.models.length + ' models available.')) + '</div>';
        }
        return '<div class="model-status-indicator model-status-error">❌ <strong>Connection failed</strong> — ' + escapeHtml(ms.error || 'Unknown error') + '<br><span class="helper-text">' + escapeHtml(ms.diagnostics || '') + '</span></div>';
    }

    function renderProviderFetchStatus(fs){
        if(!fs) return '';
        if(fs.type === 'loading'){
            return '<div class="model-status-indicator model-status-loading" style="margin-top:0.5rem"><span class="loading-indicator" aria-hidden="true"><span></span><span></span><span></span></span> <span>Connecting to ' + escapeHtml(fs.provider || 'provider') + ' at ' + escapeHtml(fs.endpoint || '') + '\u2026</span></div>';
        }
        if(fs.type === 'success'){
            return '<div class="model-status-indicator model-status-success" style="margin-top:0.5rem">\u2705 Connected! Found ' + fs.count + ' models available.</div>';
        }
        if(fs.type === 'error'){
            return '<div class="model-status-indicator model-status-error" style="margin-top:0.5rem">\u274c <strong>Could not fetch models</strong> \u2014 ' + escapeHtml(fs.error || 'Unknown error') + (fs.diagnostics ? '<br><span class="helper-text">' + escapeHtml(fs.diagnostics) + '</span>' : '') + '</div>';
        }
        return '';
    }

    function fetchProviderModels(context, provider, endpoint, apiKey){
        var state = dashboardState.llm;
        var statusKey = context === 'add' ? 'addFetchStatus' : 'editFetchStatus';
        var modelsKey = context === 'add' ? 'addFetchedModels' : 'editFetchedModels';

        state[statusKey] = {type:'loading',provider:provider,endpoint:endpoint};
        state[modelsKey] = null;
        renderLlmSettingsPanels();

        var queryParams = ['provider=' + encodeURIComponent(provider)];
        if(endpoint) queryParams.push('endpoint=' + encodeURIComponent(endpoint));
        if(apiKey) queryParams.push('apiKey=' + encodeURIComponent(apiKey));
        var url = '/api/system/models?' + queryParams.join('&');

        apiRequest(url).then(function(result){
            if(result.success && result.models && result.models.length > 0){
                state[statusKey] = {type:'success',count:result.models.length};
                state[modelsKey] = result.models;
                if(context === 'add' && state.addDraft && !state.addDraft.modelId){
                    state.addDraft.modelId = result.models[0];
                }
                if(context === 'edit' && state.editDraft && !state.editDraft.modelId){
                    state.editDraft.modelId = result.models[0];
                }
            } else {
                state[statusKey] = {type:'error',error:result.error || 'No models returned.',diagnostics:result.diagnostics || ''};
            }
        }).catch(function(error){
            state[statusKey] = {type:'error',error:error.message,diagnostics:'Request failed: ' + error.message};
        }).finally(function(){
            renderLlmSettingsPanels();
        });
    }

    function buildFullPayload(defaultProvider, providers){
        return {
            defaultProvider:defaultProvider,
            providers:providers.map(function(p){
                var entry = {name:p.name,modelId:p.modelId,endpoint:p.endpoint};
                if(p.displayName) entry.displayName = p.displayName;
                if(p.type) entry.type = p.type;
                if(p.apiKey) entry.apiKey = p.apiKey;
                return entry;
            })
        };
    }

    function submitLlmConfig(payload){
        var state = dashboardState.llm;
        state.isSaving = true;
        state.error = '';
        state.message = '';
        renderLlmSettingsPanels();

        return apiRequest('/api/system/llm-config', {
            method:'PUT',
            body:JSON.stringify(payload)
        }).then(function(result){
            state.config = result && result.configuration ? result.configuration : state.config;
            state.message = result && result.message
                ? result.message
                : 'LLM settings saved successfully.';
            state.editingProvider = null;
            state.editDraft = null;
            state.addingProvider = false;
            state.addDraft = null;
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isSaving = false;
            renderLlmSettingsPanels();
        });
    }

    function saveNewProvider(){
        var state = dashboardState.llm;
        var nameSelect = document.getElementById('llmNewProviderSelect');
        var modelSelect = document.getElementById('llmNewModelSelect');
        var modelInput = document.getElementById('llmNewModelInput');
        var modelCustom = document.getElementById('llmNewModelCustom');
        var endpointInput = document.getElementById('llmNewEndpointInput');
        if(!nameSelect || !endpointInput){
            return;
        }

        var providerType = nameSelect.value;
        var displayNameInput = document.getElementById('llmNewDisplayNameInput');
        var apiKeyInput = document.getElementById('llmNewApiKeyInput');
        var modelId = '';
        if(modelSelect){
            modelId = modelSelect.value === '__custom__'
                ? (modelCustom ? modelCustom.value.trim() : '')
                : modelSelect.value;
        } else if(modelInput){
            modelId = modelInput.value.trim();
        }
        var endpoint = endpointInput.value.trim();
        var displayName = displayNameInput ? displayNameInput.value.trim() : '';
        var apiKey = apiKeyInput ? apiKeyInput.value.trim() : '';
        if(!providerType || !modelId || !endpoint){
            state.error = 'Provider type, model, and endpoint are required.';
            renderLlmSettingsPanels();
            return;
        }

        // Auto-generate unique instance name from type.
        var config = state.config || {defaultProvider:'unknown',providers:[]};
        var existingNames = (config.providers || []).map(function(p){ return p.name; });
        var baseName = providerType;
        var instanceName = baseName;
        var counter = 2;
        while(existingNames.indexOf(instanceName) >= 0){
            instanceName = baseName + '-' + counter;
            counter++;
        }

        var providers = (config.providers || []).slice();
        providers.push({name:instanceName,displayName:displayName||instanceName,type:providerType,modelId:modelId,endpoint:endpoint,apiKey:apiKey});
        var payload = buildFullPayload(config.defaultProvider, providers);
        submitLlmConfig(payload);
    }

    function saveEditedProvider(providerName){
        var state = dashboardState.llm;
        var modelSelect = document.querySelector('.llm-edit-model-select[data-provider="'+providerName+'"]');
        var modelInput = document.querySelector('.llm-edit-model[data-provider="'+providerName+'"]');
        var modelCustom = document.querySelector('.llm-edit-model-custom[data-provider="'+providerName+'"]');
        var endpointInput = document.querySelector('.llm-edit-endpoint[data-provider="'+providerName+'"]');
        if(!endpointInput){
            return;
        }

        var modelId = '';
        if(modelSelect){
            modelId = modelSelect.value === '__custom__'
                ? (modelCustom ? modelCustom.value.trim() : '')
                : modelSelect.value;
        } else if(modelInput){
            modelId = modelInput.value.trim();
        }
        var endpoint = endpointInput.value.trim();
        if(!modelId || !endpoint){
            state.error = 'Model and endpoint are required.';
            renderLlmSettingsPanels();
            return;
        }

        var displayNameInput = document.querySelector('.llm-edit-displayname[data-provider="'+providerName+'"]');
        var apiKeyInput = document.querySelector('.llm-edit-apikey[data-provider="'+providerName+'"]');
        var displayName = displayNameInput ? displayNameInput.value.trim() : '';
        var apiKey = apiKeyInput ? apiKeyInput.value.trim() : '';

        var config = state.config || {defaultProvider:'unknown',providers:[]};
        var providers = (config.providers || []).map(function(p){
            if(p.name === providerName){
                return {name:p.name,displayName:displayName||p.displayName||'',type:p.type||p.name,modelId:modelId,endpoint:endpoint,apiKey:apiKey};
            }
            return p;
        });
        var payload = buildFullPayload(config.defaultProvider, providers);
        submitLlmConfig(payload);
    }

    function setDefaultProvider(providerName){
        var state = dashboardState.llm;
        var config = state.config || {defaultProvider:'unknown',providers:[]};
        var payload = buildFullPayload(providerName, config.providers || []);
        submitLlmConfig(payload);
    }

    function removeProvider(providerName){
        var state = dashboardState.llm;
        var config = state.config || {defaultProvider:'unknown',providers:[]};
        var providers = (config.providers || []).filter(function(p){ return p.name !== providerName; });
        if(providers.length === 0){
            state.error = 'Cannot remove the last provider.';
            renderLlmSettingsPanels();
            return;
        }
        var defaultProv = config.defaultProvider;
        if(defaultProv === providerName){
            defaultProv = providers[0].name;
        }
        var payload = buildFullPayload(defaultProv, providers);
        submitLlmConfig(payload);
    }

    // Usage page.
    function renderUsagePage(){
        queuePageInit(initUsagePage);
        var st = dashboardState.usage;
        return `
<!-- Page: Usage -->
<section class='page-shell'>
    <header class='page-header'>
        <div>
            <h1>Usage</h1>
            <p>LLM usage statistics and cost tracking across all providers and agents.</p>
        </div>
        <span class='pill'>Cost tracking</span>
    </header>
    <div id='usageContent'>${st.loading ? renderLoadingPanel('Loading usage statistics\u2026') : ''}</div>
</section>`;
    }

    function initUsagePage(){
        renderUsagePanels();
        if(!dashboardState.usage.data){
            loadUsageData(false);
        }
    }

    function loadUsageData(force){
        var state = dashboardState.usage;
        if(state.loading){
            return Promise.resolve();
        }
        if(!force && state.data){
            renderUsagePanels();
            return Promise.resolve();
        }

        state.loading = true;
        state.error = '';
        renderUsagePanels();

        return apiRequest('/api/system/llm-usage').then(function(result){
            state.data = result;
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.loading = false;
            renderUsagePanels();
        });
    }

    function getUsagePeriodData(){
        var state = dashboardState.usage;
        var data = state.data;
        if(!data) return null;
        var p = state.period;
        if(p === '24h') return data.last24Hours || null;
        if(p === '7d') return data.last7Days || null;
        if(p === '30d') return data.last30Days || null;
        return null;
    }

    function renderUsagePanels(){
        var el = document.getElementById('usageContent');
        if(!el) return;

        var state = dashboardState.usage;
        var data = state.data;

        if(state.loading && !data){
            el.innerHTML = renderLoadingPanel('Loading usage statistics\u2026');
            return;
        }
        if(!data){
            el.innerHTML = renderStatusBanner(state.error, 'error') + renderSettingsEmptyState(
                'Usage data unavailable',
                'No usage summary could be loaded from the server.',
                'Retry',
                "id='usageRetryButton'");
            var retryBtn = document.getElementById('usageRetryButton');
            if(retryBtn) retryBtn.onclick = function(){ loadUsageData(true); };
            return;
        }

        var period = state.period;
        var periodData = getUsagePeriodData();
        var displayCalls = period === 'all' ? data.callCount : (periodData ? periodData.calls : 0);
        var displayCost = period === 'all' ? data.totalCostUsd : (periodData ? periodData.costUsd : 0);
        var displayInput = period === 'all' ? data.inputTokens : 0;
        var displayOutput = period === 'all' ? data.outputTokens : 0;

        var html = '';

        // Period pills
        html += '<div style="display:flex;gap:0.5rem;flex-wrap:wrap;margin-bottom:1rem">';
        var periods = [{key:'24h',label:'24h'},{key:'7d',label:'7d'},{key:'30d',label:'30d'},{key:'all',label:'All Time'}];
        for(var pi=0;pi<periods.length;pi++){
            var p = periods[pi];
            var active = period === p.key;
            html += '<button type="button" class="' + (active ? 'primary-button' : 'secondary-button') + '" data-usage-period="' + p.key + '" style="font-size:0.85em;padding:0.3rem 0.75rem">' + p.label + '</button>';
        }
        html += '<div style="flex:1"></div>';
        html += '<button type="button" class="secondary-button" id="usageRefreshButton" style="font-size:0.85em;padding:0.3rem 0.75rem">Refresh</button>';
        html += '</div>';

        // Summary cards
        html += '<div class="stats-grid">';
        html += '<div class="stat-card"><div class="stat-label">Total calls</div><div class="stat-value">' + formatNumber(displayCalls) + '</div></div>';
        if(period === 'all'){
            html += '<div class="stat-card"><div class="stat-label">Input tokens</div><div class="stat-value">' + formatNumber(displayInput) + '</div></div>';
            html += '<div class="stat-card"><div class="stat-label">Output tokens</div><div class="stat-value">' + formatNumber(displayOutput) + '</div></div>';
        }
        html += '<div class="stat-card"><div class="stat-label">Estimated cost</div><div class="stat-value">' + formatCurrency(displayCost) + '</div></div>';
        html += '</div>';

        // Time period overview (only show when viewing "all")
        if(period === 'all' && (data.last24Hours || data.last7Days || data.last30Days)){
            html += '<h3 style="margin:1.25rem 0 0.5rem">Activity by period</h3>';
            html += '<div class="stats-grid">';
            if(data.last24Hours){
                html += '<div class="stat-card"><div class="stat-label">Last 24 hours</div><div class="stat-value">' + formatNumber(data.last24Hours.calls) + ' calls</div><div class="stat-sub">' + formatCurrency(data.last24Hours.costUsd) + '</div></div>';
            }
            if(data.last7Days){
                html += '<div class="stat-card"><div class="stat-label">Last 7 days</div><div class="stat-value">' + formatNumber(data.last7Days.calls) + ' calls</div><div class="stat-sub">' + formatCurrency(data.last7Days.costUsd) + '</div></div>';
            }
            if(data.last30Days){
                html += '<div class="stat-card"><div class="stat-label">Last 30 days</div><div class="stat-value">' + formatNumber(data.last30Days.calls) + ' calls</div><div class="stat-sub">' + formatCurrency(data.last30Days.costUsd) + '</div></div>';
            }
            html += '</div>';
        }

        // By Provider/Model table
        if(data.byProvider && data.byProvider.length > 0){
            html += '<section class="card settings-card" style="margin-top:1.25rem">';
            html += '<div class="section-heading"><div><h2>By provider / model</h2><p>Combined usage when multiple agents share the same provider and model.</p></div></div>';
            html += '<div style="overflow-x:auto">';
            html += '<table class="data-table" style="width:100%;border-collapse:collapse;font-size:0.92em">';
            html += '<thead><tr>';
            html += '<th style="text-align:left;padding:0.4rem 0.6rem">Provider</th>';
            html += '<th style="text-align:left;padding:0.4rem 0.6rem">Model</th>';
            html += '<th style="text-align:right;padding:0.4rem 0.6rem">Calls</th>';
            html += '<th style="text-align:right;padding:0.4rem 0.6rem">Input tokens</th>';
            html += '<th style="text-align:right;padding:0.4rem 0.6rem">Output tokens</th>';
            html += '<th style="text-align:right;padding:0.4rem 0.6rem">Cost</th>';
            html += '<th style="text-align:left;padding:0.4rem 0.6rem;width:25%"></th>';
            html += '</tr></thead><tbody>';

            var maxCost = 0;
            for(var mi=0;mi<data.byProvider.length;mi++){
                if(data.byProvider[mi].estimatedCostUsd > maxCost) maxCost = data.byProvider[mi].estimatedCostUsd;
            }

            for(var ri=0;ri<data.byProvider.length;ri++){
                var row = data.byProvider[ri];
                var pct = maxCost > 0 ? Math.round((row.estimatedCostUsd / maxCost) * 100) : 0;
                html += '<tr>';
                html += '<td style="padding:0.4rem 0.6rem">' + escapeHtml(row.provider) + '</td>';
                html += '<td style="padding:0.4rem 0.6rem"><code>' + escapeHtml(row.model) + '</code></td>';
                html += '<td style="text-align:right;padding:0.4rem 0.6rem">' + formatNumber(row.calls) + '</td>';
                html += '<td style="text-align:right;padding:0.4rem 0.6rem">' + formatNumber(row.inputTokens) + '</td>';
                html += '<td style="text-align:right;padding:0.4rem 0.6rem">' + formatNumber(row.outputTokens) + '</td>';
                html += '<td style="text-align:right;padding:0.4rem 0.6rem">' + formatCurrency(row.estimatedCostUsd) + '</td>';
                html += '<td style="padding:0.4rem 0.6rem"><div style="background:var(--bg-tertiary);border-radius:4px;height:8px;overflow:hidden"><div style="background:var(--accent);height:100%;width:' + pct + '%"></div></div></td>';
                html += '</tr>';
            }
            html += '</tbody></table></div></section>';
        }

        // By Agent table
        if(data.byAgent && data.byAgent.length > 0){
            html += '<section class="card settings-card" style="margin-top:1.25rem">';
            html += '<div class="section-heading"><div><h2>By agent</h2><p>Per-agent breakdown showing which agent consumed which provider and model.</p></div></div>';
            html += '<div style="overflow-x:auto">';
            html += '<table class="data-table" style="width:100%;border-collapse:collapse;font-size:0.92em">';
            html += '<thead><tr>';
            html += '<th style="text-align:left;padding:0.4rem 0.6rem">Agent</th>';
            html += '<th style="text-align:left;padding:0.4rem 0.6rem">Provider</th>';
            html += '<th style="text-align:left;padding:0.4rem 0.6rem">Model</th>';
            html += '<th style="text-align:right;padding:0.4rem 0.6rem">Calls</th>';
            html += '<th style="text-align:right;padding:0.4rem 0.6rem">Input tokens</th>';
            html += '<th style="text-align:right;padding:0.4rem 0.6rem">Output tokens</th>';
            html += '<th style="text-align:right;padding:0.4rem 0.6rem">Cost</th>';
            html += '</tr></thead><tbody>';

            // Group by agent name
            var agentGroups = {};
            var agentOrder = [];
            for(var ai=0;ai<data.byAgent.length;ai++){
                var ag = data.byAgent[ai];
                if(!agentGroups[ag.agentName]){
                    agentGroups[ag.agentName] = {rows:[],totalCalls:0,totalInput:0,totalOutput:0,totalCost:0};
                    agentOrder.push(ag.agentName);
                }
                agentGroups[ag.agentName].rows.push(ag);
                agentGroups[ag.agentName].totalCalls += ag.calls;
                agentGroups[ag.agentName].totalInput += ag.inputTokens;
                agentGroups[ag.agentName].totalOutput += ag.outputTokens;
                agentGroups[ag.agentName].totalCost += ag.estimatedCostUsd;
            }

            // Sort agents by total cost descending
            agentOrder.sort(function(a,b){ return agentGroups[b].totalCost - agentGroups[a].totalCost; });

            for(var gi=0;gi<agentOrder.length;gi++){
                var agentName = agentOrder[gi];
                var group = agentGroups[agentName];
                var expanded = !!state.expandedAgents[agentName];
                var toggleAttr = 'data-usage-toggle-agent="' + escapeHtml(agentName) + '"';

                // Agent header row
                html += '<tr style="background:var(--bg-secondary);cursor:pointer" ' + toggleAttr + '>';
                html += '<td style="padding:0.4rem 0.6rem;font-weight:600" colspan="3">' + (expanded ? '\u25BC' : '\u25B6') + ' ' + escapeHtml(agentName) + '</td>';
                html += '<td style="text-align:right;padding:0.4rem 0.6rem;font-weight:600">' + formatNumber(group.totalCalls) + '</td>';
                html += '<td style="text-align:right;padding:0.4rem 0.6rem;font-weight:600">' + formatNumber(group.totalInput) + '</td>';
                html += '<td style="text-align:right;padding:0.4rem 0.6rem;font-weight:600">' + formatNumber(group.totalOutput) + '</td>';
                html += '<td style="text-align:right;padding:0.4rem 0.6rem;font-weight:600">' + formatCurrency(group.totalCost) + '</td>';
                html += '</tr>';

                // Detail rows (collapsible)
                if(expanded){
                    for(var di=0;di<group.rows.length;di++){
                        var dr = group.rows[di];
                        html += '<tr style="color:var(--text-secondary)">';
                        html += '<td style="padding:0.4rem 0.6rem 0.4rem 1.5rem"></td>';
                        html += '<td style="padding:0.4rem 0.6rem">' + escapeHtml(dr.provider) + '</td>';
                        html += '<td style="padding:0.4rem 0.6rem"><code>' + escapeHtml(dr.model) + '</code></td>';
                        html += '<td style="text-align:right;padding:0.4rem 0.6rem">' + formatNumber(dr.calls) + '</td>';
                        html += '<td style="text-align:right;padding:0.4rem 0.6rem">' + formatNumber(dr.inputTokens) + '</td>';
                        html += '<td style="text-align:right;padding:0.4rem 0.6rem">' + formatNumber(dr.outputTokens) + '</td>';
                        html += '<td style="text-align:right;padding:0.4rem 0.6rem">' + formatCurrency(dr.estimatedCostUsd) + '</td>';
                        html += '</tr>';
                    }
                }
            }
            html += '</tbody></table></div></section>';
        }

        html += '<p class="helper-text" style="margin-top:1rem">Cost estimates are approximate and based on published provider pricing.</p>';

        el.innerHTML = html;

        // Bind period pill clicks
        var periodButtons = document.querySelectorAll('[data-usage-period]');
        for(var bi=0;bi<periodButtons.length;bi++){
            periodButtons[bi].onclick = function(){
                dashboardState.usage.period = this.getAttribute('data-usage-period');
                renderUsagePanels();
            };
        }

        // Bind refresh
        var refreshBtn = document.getElementById('usageRefreshButton');
        if(refreshBtn) refreshBtn.onclick = function(){ loadUsageData(true); };

        // Bind agent toggle clicks
        var toggleButtons = document.querySelectorAll('[data-usage-toggle-agent]');
        for(var ti=0;ti<toggleButtons.length;ti++){
            toggleButtons[ti].onclick = function(){
                var name = this.getAttribute('data-usage-toggle-agent');
                if(dashboardState.usage.expandedAgents[name]){
                    delete dashboardState.usage.expandedAgents[name];
                } else {
                    dashboardState.usage.expandedAgents[name] = true;
                }
                renderUsagePanels();
            };
        }
    }

    // Agents page.
    function getActiveAgentConfigName(){
        var hash = window.location.hash;
        var prefix = '#/agents/';
        if(hash.indexOf(prefix) === 0){
            return decodeURIComponent(hash.substring(prefix.length).split('/')[0]);
        }
        return '';
    }

    function renderAgentsPage(){
        var agentName = getActiveAgentConfigName();
        if(agentName){
            queuePageInit(function(){ initAgentConfigPage(agentName); });
            return renderAgentConfigPage(agentName);
        }
        queuePageInit(initAgentsPage);
        return `
<!-- Page: Agents -->
<section class='page-shell'>
    <header class='page-header'>
        <div>
            <h1>Agents</h1>
            <p>Inspect available agent definitions, see autonomy defaults, and expand cards to review prompts and attached skills.</p>
        </div>
        <span class='pill'>Agent catalog</span>
    </header>
    <div id='orchSettingsSection'></div>
    <div class='catalog-list' id='agentsList'>${renderLoadingPanel('Loading agents\u2026')}</div>
</section>`;
    }

    function initAgentsPage(){
        renderAgentsList();
        renderOrchestratorSettings();
        if(!dashboardState.agents.items){
            loadAgents(false);
        }
        if(!dashboardState.agents.orchConfig){
            loadOrchestratorConfig();
        }
    }

    function loadAgents(force){
        var state = dashboardState.agents;
        if(state.isLoading){
            return Promise.resolve();
        }
        if(!force && state.items){
            renderAgentsList();
            return Promise.resolve();
        }

        state.isLoading = true;
        state.error = '';
        renderAgentsList();

        return apiRequest('/api/agents').then(function(items){
            state.items = items || [];
            loadSystemAgentPrefs();
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isLoading = false;
            renderAgentsList();
        });
    }

    function loadSystemAgentPrefs(){
        var state = dashboardState.agents;
        apiRequest('/api/preferences/agent').then(function(prefs){
            if(prefs){
                var dn = prefs['agent.assistant.displayName'];
                var tn = prefs['agent.assistant.tone'];
                if(dn && dn.value) state.systemPrefs.displayName = dn.value;
                if(tn && tn.value) state.systemPrefs.tone = tn.value;
            }
            renderAgentsList();
        }).catch(function(){
            // Preferences may not exist yet; use defaults.
        });
    }

    function loadAgentDetail(name){
        var state = dashboardState.agents;
        if(state.detailLoading[name]){
            return Promise.resolve();
        }

        state.detailLoading[name] = true;
        state.detailError[name] = '';
        renderAgentsList();

        return apiRequest('/api/agents/' + encodeURIComponent(name)).then(function(detail){
            state.detailByName[name] = detail;
        }).catch(function(error){
            state.detailError[name] = error.message;
        }).finally(function(){
            state.detailLoading[name] = false;
            renderAgentsList();
        });
    }

    function toggleAgentExpansion(name){
        var state = dashboardState.agents;
        state.expandedName = state.expandedName === name ? '' : name;
        if(state.expandedName !== name) state.editingSystem = false;
        renderAgentsList();
        if(state.expandedName === name && !state.detailByName[name]){
            loadAgentDetail(name);
        }
    }

    function renderAgentDetail(summary, detail, isLoading, error){
        if(isLoading){
            return `<div class='expand-panel'>${renderLoadingPanel('Loading agent details\u2026')}</div>`;
        }
        if(error){
            return `<div class='expand-panel'>${renderStatusBanner(error, 'error')}<div class='settings-actions'><button type='button' class='secondary-button' data-agent-detail-retry='${escapeHtml(summary.name)}'>Retry</button></div></div>`;
        }
        if(!detail){
            return '';
        }

        var capabilities = detail.llmRequirements && detail.llmRequirements.capabilities
            ? detail.llmRequirements.capabilities
            : [];
        var html = `
<div class='expand-panel'>
    <div class='detail-card'>
        <h3>System prompt preview</h3>
        <pre class='detail-code'>${escapeHtml(truncateText(detail.systemPrompt || 'No system prompt provided.', 1600))}</pre>
    </div>
    <div class='detail-columns'>
        <div class='detail-card'>
            <h3>Skills</h3>
            ${renderTokens(detail.skills || [], 'No skills referenced by this agent.')}
        </div>
        <div class='detail-card'>
            <h3>LLM requirements</h3>
            ${renderTokens(capabilities, 'No explicit LLM capabilities declared.')}
        </div>
    </div>`;
        if(summary.isSystem){
            html += renderSystemAgentCustomization();
        }
        html += `</div>`;
        return html;
    }

    var agentToneOptions = ['Professional','Friendly','Concise','Detailed'];

    function renderSystemAgentCustomization(){
        var state = dashboardState.agents;
        if(!state.editingSystem){
            return `<div class='settings-actions' style='margin-top:1rem;'><button type='button' class='secondary-button' data-agents-action='edit-system'>Edit preferences</button></div>`;
        }
        var toneSelect = '<select id="agentToneSelect" class="settings-input">';
        for(var i=0;i<agentToneOptions.length;i++){
            var sel = agentToneOptions[i] === state.systemPrefs.tone ? ' selected' : '';
            toneSelect += '<option value="' + escapeHtml(agentToneOptions[i]) + '"' + sel + '>' + escapeHtml(agentToneOptions[i]) + '</option>';
        }
        toneSelect += '</select>';
        var banner = '';
        if(state.systemPrefsMessage) banner = renderStatusBanner(state.systemPrefsMessage, 'success');
        if(state.systemPrefsError) banner = renderStatusBanner(state.systemPrefsError, 'error');
        return `
<div class='detail-card' style='margin-top:1rem;'>
    <h3>Customize assistant</h3>
    ${banner}
    <div class='form-group'>
        <label class='settings-label' for='agentDisplayNameInput'>Display name</label>
        <input type='text' id='agentDisplayNameInput' class='settings-input' placeholder='e.g. Jarvis' value='${escapeHtml(state.systemPrefs.displayName)}' />
    </div>
    <div class='form-group'>
        <label class='settings-label' for='agentToneSelect'>Chat tone</label>
        ${toneSelect}
    </div>
    <div class='settings-actions'>
        <button type='button' class='primary-button' data-agents-action='save-system' ${state.systemPrefsSaving ? 'disabled' : ''}>${state.systemPrefsSaving ? 'Saving\u2026' : 'Save'}</button>
        <button type='button' class='secondary-button' data-agents-action='cancel-system'>Cancel</button>
    </div>
</div>`;
    }

    function saveSystemAgentPrefs(){
        var state = dashboardState.agents;
        var displayName = (document.getElementById('agentDisplayNameInput') || {}).value || '';
        var tone = (document.getElementById('agentToneSelect') || {}).value || 'Professional';
        state.systemPrefsSaving = true;
        state.systemPrefsMessage = '';
        state.systemPrefsError = '';
        renderAgentsList();

        var p1 = upsertPreference('agent.assistant.displayName', displayName.trim(), 'agent');
        var p2 = upsertPreference('agent.assistant.tone', tone, 'agent');
        Promise.all([p1, p2]).then(function(){
            state.systemPrefs.displayName = displayName.trim();
            state.systemPrefs.tone = tone;
            state.systemPrefsMessage = 'Preferences saved.';
            state.editingSystem = false;
        }).catch(function(err){
            state.systemPrefsError = err.message || 'Failed to save preferences.';
        }).finally(function(){
            state.systemPrefsSaving = false;
            renderAgentsList();
        });
    }

    function renderAgentsList(){
        var container = document.getElementById('agentsList');
        if(!container){
            return;
        }

        var state = dashboardState.agents;
        var html = '';
        if(state.error){
            html += renderStatusBanner(state.error, 'error');
        }

        if(state.isLoading && !state.items){
            container.innerHTML = html + renderLoadingPanel('Loading agents\u2026');
            container.onclick = handleAgentsClick;
            return;
        }

        if(!state.items || state.items.length === 0){
            container.innerHTML = html + renderSettingsEmptyState(
                'No agents configured.',
                'Add agent definition files to the agents directory.',
                'Reload agents',
                "data-agents-action='reload'");
            container.onclick = handleAgentsClick;
            return;
        }

        for(var i=0;i<state.items.length;i++){
            var summary = state.items[i];
            var expanded = state.expandedName === summary.name;
            var systemBadge = summary.isSystem ? " <span class='pill' style='background:var(--accent);color:#000;font-size:0.7rem;'>\uD83D\uDD12 System</span>" : '';
            var displayTitle = summary.isSystem && state.systemPrefs.displayName
                ? escapeHtml(state.systemPrefs.displayName) + " <span style='opacity:0.5;font-size:0.85em;'>(" + escapeHtml(summary.name) + ")</span>"
                : escapeHtml(summary.name);
            var priorityBadge = typeof summary.priority === 'number' ? "<span class='badge is-muted'>Priority " + summary.priority + "</span>" : '';
            var budgetBadge = summary.hasBudget ? "<span class='badge is-muted'>\uD83D\uDFE1 Budget</span>" : "<span class='badge is-muted'>\uD83D\uDFE2 No budget</span>";
            var llmBadge = summary.hasLlmOverride ? "<span class='badge is-accent'>\u26A1 Custom LLM</span>" : '';
            var triggerBadge = summary.triggerCount > 0 ? renderCountBadge(summary.triggerCount, 'trigger') : '';
            html += `
<article class='card catalog-card'>
    <div class='catalog-summary'>
        <button type='button' class='catalog-trigger' data-agent-name='${escapeHtml(summary.name)}' aria-expanded='${expanded ? 'true' : 'false'}' style='flex:1'>
            <div>
                <h2 class='catalog-title'>${displayTitle}${systemBadge}</h2>
                <p class='catalog-description'>${escapeHtml(summary.description || 'No description provided.')}</p>
                <div class='agent-card-badges'>
                    ${renderAutonomyBadge(summary.autonomyLevel)}
                    ${renderCountBadge(summary.skillCount, 'skill')}
                    ${priorityBadge}
                    ${budgetBadge}
                    ${llmBadge}
                    ${triggerBadge}
                </div>
            </div>
        </button>
        <div class='agent-card-actions'>
            <a href='#/agents/${encodeURIComponent(summary.name)}' class='secondary-button' style='white-space:nowrap;min-height:2.2rem;padding:0 .75rem;font-size:.84rem;'>Configure</a>
            <span class='expand-icon' aria-hidden='true' style='cursor:pointer' data-agent-name='${escapeHtml(summary.name)}'>${expanded ? '\u2212' : '+'}</span>
        </div>
    </div>
    ${expanded ? renderAgentDetail(summary, state.detailByName[summary.name], !!state.detailLoading[summary.name], state.detailError[summary.name]) : ''}
</article>`;
        }

        container.innerHTML = html;
        container.onclick = handleAgentsClick;
    }

    function handleAgentsClick(event){
        var reloadButton = event.target.closest('[data-agents-action="reload"]');
        if(reloadButton){
            loadAgents(true);
            return;
        }

        var retryButton = event.target.closest('[data-agent-detail-retry]');
        if(retryButton){
            loadAgentDetail(retryButton.getAttribute('data-agent-detail-retry'));
            return;
        }

        var editBtn = event.target.closest('[data-agents-action="edit-system"]');
        if(editBtn){
            dashboardState.agents.editingSystem = true;
            dashboardState.agents.systemPrefsMessage = '';
            dashboardState.agents.systemPrefsError = '';
            renderAgentsList();
            return;
        }

        var cancelBtn = event.target.closest('[data-agents-action="cancel-system"]');
        if(cancelBtn){
            dashboardState.agents.editingSystem = false;
            renderAgentsList();
            return;
        }

        var saveBtn = event.target.closest('[data-agents-action="save-system"]');
        if(saveBtn){
            saveSystemAgentPrefs();
            return;
        }

        var trigger = event.target.closest('[data-agent-name]');
        if(trigger){
            toggleAgentExpansion(trigger.getAttribute('data-agent-name'));
        }
    }

    // Agent configuration page (detail/edit view).
    function renderAgentConfigPage(agentName){
        return `
<section class='page-shell'>
    <header class='page-header'>
        <div>
            <h1 id='agentConfigTitle'>Agent Configuration</h1>
            <p id='agentConfigDesc'>Loading agent details\u2026</p>
        </div>
        <a href='#/agents' class='secondary-button'>\u2190 Back to Agents</a>
    </header>
    <div id='agentConfigStatus'></div>
    <div id='agentConfigTabBar'></div>
    <section class='card settings-card' id='agentConfigCard'>${renderLoadingPanel('Loading agent configuration\u2026')}</section>
</section>`;
    }

    function initAgentConfigPage(agentName){
        var state = dashboardState.agents;
        state.configDetail = null;
        state.configBudget = null;
        state.configTab = 'overview';
        state.configLoading = false;
        state.configSaving = false;
        state.configError = '';
        state.configMessage = '';
        loadAgentConfig(agentName);
    }

    function loadAgentConfig(agentName){
        var state = dashboardState.agents;
        state.configLoading = true;
        state.configError = '';
        renderAgentConfigCard();

        apiRequest('/api/agents/' + encodeURIComponent(agentName)).then(function(detail){
            state.configDetail = detail;
            var titleEl = document.getElementById('agentConfigTitle');
            var descEl = document.getElementById('agentConfigDesc');
            if(titleEl) titleEl.textContent = (detail.name || agentName) + ' Configuration';
            if(descEl) descEl.textContent = detail.description || '';
        }).catch(function(error){
            state.configError = error.message;
        }).finally(function(){
            state.configLoading = false;
            renderAgentConfigCard();
            loadAgentBudget(agentName);
        });
    }

    function loadAgentBudget(agentName){
        var state = dashboardState.agents;
        apiRequest('/api/agents/' + encodeURIComponent(agentName) + '/budget').then(function(budget){
            state.configBudget = budget;
            if(state.configTab === 'budget'){
                renderAgentConfigCard();
            }
        }).catch(function(){
            state.configBudget = null;
        });
    }

    var agentConfigTabs = [
        {id:'overview', icon:'\uD83D\uDCCB', label:'Overview'},
        {id:'llm', icon:'\uD83E\uDD16', label:'LLM Config'},
        {id:'permissions', icon:'\uD83D\uDD10', label:'Permissions'},
        {id:'budget', icon:'\uD83D\uDCB0', label:'Budget'},
        {id:'triggers', icon:'\u26A1', label:'Triggers & Restrictions'}
    ];

    function renderAgentConfigTabBar(){
        var state = dashboardState.agents;
        var agentName = getActiveAgentConfigName();
        var tabs = [];
        for(var i=0;i<agentConfigTabs.length;i++){
            var t = agentConfigTabs[i];
            tabs.push({id:t.id, href:'javascript:void(0)', icon:t.icon, label:t.label});
        }
        return renderTabBar(tabs, state.configTab);
    }

    function renderAgentConfigCard(){
        var card = document.getElementById('agentConfigCard');
        var statusHost = document.getElementById('agentConfigStatus');
        var tabBarHost = document.getElementById('agentConfigTabBar');
        if(!card) return;
        var state = dashboardState.agents;

        if(statusHost){
            var bannerHtml = '';
            if(state.configError) bannerHtml = renderStatusBanner(state.configError, 'error');
            else if(state.configMessage) bannerHtml = renderStatusBanner(state.configMessage, 'success');
            statusHost.innerHTML = bannerHtml;
        }

        if(state.configLoading && !state.configDetail){
            if(tabBarHost) tabBarHost.innerHTML = '';
            card.innerHTML = renderLoadingPanel('Loading agent configuration\u2026');
            return;
        }
        if(!state.configDetail){
            if(tabBarHost) tabBarHost.innerHTML = '';
            card.innerHTML = renderSettingsEmptyState('Agent not found', 'Could not load configuration for this agent.');
            return;
        }

        if(tabBarHost){
            tabBarHost.innerHTML = renderAgentConfigTabBar();
            tabBarHost.onclick = function(e){
                var tabLink = e.target.closest('.tab');
                if(tabLink){
                    e.preventDefault();
                    var tabId = '';
                    for(var i=0;i<agentConfigTabs.length;i++){
                        if(tabLink.textContent.indexOf(agentConfigTabs[i].label) >= 0){
                            tabId = agentConfigTabs[i].id;
                            break;
                        }
                    }
                    if(tabId && tabId !== state.configTab){
                        state.configTab = tabId;
                        state.configMessage = '';
                        state.configError = '';
                        renderAgentConfigCard();
                    }
                }
            };
        }

        var detail = state.configDetail;
        var isSystem = !!detail.isSystem;
        var isSaving = state.configSaving;
        var disabledAttr = isSaving ? ' disabled' : '';
        var html = '';

        if(isSystem){
            html += "<div class='agent-notice'>\uD83D\uDD12 This is a system agent. Name and description cannot be changed.</div>";
        }

        var tab = state.configTab;
        if(tab === 'overview'){
            html += renderAgentOverviewTab(detail, isSystem, disabledAttr);
        } else if(tab === 'llm'){
            html += renderAgentLlmTab(detail, disabledAttr);
        } else if(tab === 'permissions'){
            html += renderAgentPermissionsTab(detail, disabledAttr);
        } else if(tab === 'budget'){
            html += renderAgentBudgetTab(detail, state.configBudget, disabledAttr);
        } else if(tab === 'triggers'){
            html += renderAgentTriggersTab(detail, disabledAttr);
        }

        html += `
<div class='settings-actions' style='margin-top:1.25rem;'>
    <button type='button' class='primary-button' id='agentConfigSaveBtn'${disabledAttr}>${isSaving ? 'Saving\u2026' : 'Save Configuration'}</button>
</div>`;

        card.innerHTML = html;

        var saveBtn = document.getElementById('agentConfigSaveBtn');
        if(saveBtn){
            saveBtn.onclick = function(){ saveAgentConfig(detail.name); };
        }
        bindAgentConfigInteractions();
    }

    function renderAgentOverviewTab(detail, isSystem, disabledAttr){
        var readOnlyAttr = isSystem ? ' disabled' : disabledAttr;
        return `
<div class='agent-config-form'>
    <div class='field-row'>
        <div class='field'>
            <span>Name</span>
            <input type='text' id='acfName' class='control' value='${escapeHtml(detail.name || '')}'${readOnlyAttr} />
        </div>
        <div class='field'>
            <span>Priority</span>
            <input type='number' id='acfPriority' class='control' value='${detail.priority != null ? detail.priority : 0}' min='0'${disabledAttr} />
        </div>
    </div>
    <div class='field'>
        <span>Description</span>
        <textarea id='acfDescription' class='control' rows='3'${readOnlyAttr}>${escapeHtml(detail.description || '')}</textarea>
    </div>
    <div class='field'>
        <span>Autonomy Level</span>
        <input type='number' id='acfAutonomy' class='control' value='${detail.autonomyLevel != null ? detail.autonomyLevel : 0}' min='0' max='4'${disabledAttr} />
    </div>
    <div class='detail-card' style='margin-top:.5rem;'>
        <h3>Skills</h3>
        ${renderTokens(detail.skills || [], 'No skills attached to this agent.')}
    </div>
</div>`;
    }

    function renderAgentLlmTab(detail, disabledAttr){
        var llmConfig = detail.llmConfig || {};
        var provider = llmConfig.provider || '';
        var model = llmConfig.model || '';
        var temperature = llmConfig.temperature != null ? llmConfig.temperature : '';
        var maxTokens = llmConfig.maxTokens != null ? llmConfig.maxTokens : '';

        var fb = llmConfig.fallback || {};
        var fbProvider = fb.provider || '';
        var fbModel = fb.model || '';
        var fbTemp = fb.temperature != null ? fb.temperature : '';
        var fbMaxTokens = fb.maxTokens != null ? fb.maxTokens : '';

        var providerOptions = "<option value=''" + (provider === '' ? ' selected' : '') + ">System Default</option>";
        var fbProviderOptions = "<option value=''" + (fbProvider === '' ? ' selected' : '') + ">None</option>";
        for(var i=0;i<llmProviders.length;i++){
            var p = llmProviders[i];
            providerOptions += "<option value='" + escapeHtml(p) + "'" + (p === provider ? ' selected' : '') + ">" + escapeHtml(p) + "</option>";
            fbProviderOptions += "<option value='" + escapeHtml(p) + "'" + (p === fbProvider ? ' selected' : '') + ">" + escapeHtml(p) + "</option>";
        }

        var hasFallback = !!(fbProvider || fbModel);

        return `
<div class='agent-config-form'>
    <p class='field-hint'>Leave fields blank to use the system default LLM configuration.</p>
    <div class='field-row'>
        <div class='field'>
            <span>Provider</span>
            <select id='acfLlmProvider' class='control'${disabledAttr}>${providerOptions}</select>
        </div>
        <div class='field'>
            <span>Model</span>
            <input type='text' id='acfLlmModel' class='control' value='${escapeHtml(model)}' placeholder='e.g. gpt-4o, llama3'${disabledAttr} />
        </div>
    </div>
    <div class='field-row'>
        <div class='field'>
            <span>Temperature (0.0 \u2013 2.0)</span>
            <input type='range' id='acfLlmTemp' class='control' min='0' max='2' step='0.1' value='${temperature !== '' ? temperature : 0.7}' style='padding:.5rem 1rem'${disabledAttr} />
            <span id='acfLlmTempVal' style='text-align:center;font-weight:600'>${temperature !== '' ? temperature : '0.7'}</span>
        </div>
        <div class='field'>
            <span>Max Tokens</span>
            <input type='number' id='acfLlmMaxTokens' class='control' value='${maxTokens}' placeholder='Leave blank for default' min='0'${disabledAttr} />
        </div>
    </div>
    <details class='fallback-section' style='margin-top:1rem;border:1px solid var(--border);border-radius:8px;padding:.75rem 1rem'${hasFallback ? ' open' : ''}>
        <summary style='cursor:pointer;font-weight:600'>\u26A0\uFE0F Fallback Configuration</summary>
        <p class='field-hint' style='margin-top:.5rem'>Used automatically when the primary provider fails or is unavailable.</p>
        <div class='field-row'>
            <div class='field'>
                <span>Fallback Provider</span>
                <select id='acfFbProvider' class='control'${disabledAttr}>${fbProviderOptions}</select>
            </div>
            <div class='field'>
                <span>Fallback Model</span>
                <input type='text' id='acfFbModel' class='control' value='${escapeHtml(fbModel)}' placeholder='e.g. gpt-4o-mini'${disabledAttr} />
            </div>
        </div>
        <div class='field-row'>
            <div class='field'>
                <span>Fallback Temperature (0.0 \u2013 2.0)</span>
                <input type='range' id='acfFbTemp' class='control' min='0' max='2' step='0.1' value='${fbTemp !== '' ? fbTemp : 0.7}' style='padding:.5rem 1rem'${disabledAttr} />
                <span id='acfFbTempVal' style='text-align:center;font-weight:600'>${fbTemp !== '' ? fbTemp : '0.7'}</span>
            </div>
            <div class='field'>
                <span>Fallback Max Tokens</span>
                <input type='number' id='acfFbMaxTokens' class='control' value='${fbMaxTokens}' placeholder='Leave blank for default' min='0'${disabledAttr} />
            </div>
        </div>
    </details>
</div>`;
    }

    function renderAgentPermissionsTab(detail, disabledAttr){
        var perms = detail.permissions || {};
        var items = [
            {id:'internet', label:'Internet Access', desc:'Allow the agent to make outbound HTTP requests.', value:!!perms.internet},
            {id:'fileSystem', label:'File System Access', desc:'Allow the agent to read and write files on disk.', value:!!perms.fileSystem},
            {id:'codeExecution', label:'Code Execution', desc:'Allow the agent to compile and run code.', value:!!perms.codeExecution},
            {id:'databaseAccess', label:'Database Access', desc:'Allow the agent to query databases directly.', value:!!perms.databaseAccess}
        ];
        var html = "<div class='agent-config-form'>";
        for(var i=0;i<items.length;i++){
            var item = items[i];
            html += `
<div class='toggle-row'>
    <label for='acfPerm_${item.id}'>
        <span>${escapeHtml(item.label)}</span>
        <span>${escapeHtml(item.desc)}</span>
    </label>
    <div class='toggle-switch'>
        <input type='checkbox' id='acfPerm_${item.id}'${item.value ? ' checked' : ''}${disabledAttr} />
        <span class='toggle-slider'></span>
    </div>
</div>`;
        }
        html += '</div>';
        return html;
    }

    function renderAgentBudgetTab(detail, budget, disabledAttr){
        var tb = detail.tokenBudget || {};
        var maxTokens = tb.maxTokensPerPeriod != null ? tb.maxTokensPerPeriod : 0;
        var period = tb.period || 'Session';
        var action = tb.actionOnExceeded || 'Stop';
        var pauseMins = tb.pauseResumeMinutes != null ? tb.pauseResumeMinutes : 5;

        var periodOptions = '';
        var periods = ['Session','Daily','Monthly'];
        for(var i=0;i<periods.length;i++){
            periodOptions += "<option value='" + periods[i] + "'" + (periods[i] === period ? ' selected' : '') + ">" + periods[i] + "</option>";
        }
        var actionOptions = '';
        var actions = ['Stop','Pause & Resume'];
        for(var i=0;i<actions.length;i++){
            var actVal = actions[i] === 'Pause & Resume' ? 'PauseResume' : actions[i];
            actionOptions += "<option value='" + actVal + "'" + (actVal === action ? ' selected' : '') + ">" + actions[i] + "</option>";
        }

        var showPauseMins = action === 'PauseResume' ? '' : ' style="display:none"';

        var html = `
<div class='agent-config-form'>
    <div class='field-row'>
        <div class='field'>
            <span>Max Tokens Per Period (0 = unlimited)</span>
            <input type='number' id='acfBudgetMax' class='control' value='${maxTokens}' min='0'${disabledAttr} />
        </div>
        <div class='field'>
            <span>Period</span>
            <select id='acfBudgetPeriod' class='control'${disabledAttr}>${periodOptions}</select>
        </div>
    </div>
    <div class='field-row'>
        <div class='field'>
            <span>Action On Exceeded</span>
            <select id='acfBudgetAction' class='control'${disabledAttr}>${actionOptions}</select>
        </div>
        <div class='field' id='acfPauseMinsField'${showPauseMins}>
            <span>Pause/Resume Minutes</span>
            <input type='number' id='acfBudgetPauseMins' class='control' value='${pauseMins}' min='1'${disabledAttr} />
        </div>
    </div>`;

        if(budget){
            var used = budget.totalTokens || 0;
            var limit = budget.maxTokensPerPeriod || 0;
            var pct = limit > 0 ? Math.min(100, Math.round((used / limit) * 100)) : 0;
            var fillClass = pct < 60 ? 'is-ok' : (pct < 85 ? 'is-warn' : 'is-danger');
            var periodLabel = '';
            if(budget.periodStart && budget.periodEnd){
                periodLabel = budget.periodStart + ' \u2013 ' + budget.periodEnd;
            }
            html += `
    <div class='detail-card' style='margin-top:.75rem;'>
        <h3>Current Usage</h3>
        <div style='margin:.5rem 0'>
            <div class='progress-track'>
                <div class='progress-fill ${fillClass}' style='width:${limit > 0 ? pct : 0}%'></div>
            </div>
            <div style='display:flex;justify-content:space-between;margin-top:.35rem;font-size:.84rem;color:var(--text-secondary)'>
                <span>${formatNumber(used)} tokens used</span>
                <span>${limit > 0 ? formatNumber(limit) + ' limit' : 'Unlimited'}</span>
            </div>
        </div>
        <div class='definition-grid' style='margin-top:.75rem;'>
            <div class='definition-item'>
                <div class='definition-label'>Input tokens</div>
                <div class='definition-value'>${formatNumber(budget.totalInputTokens || 0)}</div>
            </div>
            <div class='definition-item'>
                <div class='definition-label'>Output tokens</div>
                <div class='definition-value'>${formatNumber(budget.totalOutputTokens || 0)}</div>
            </div>
            <div class='definition-item'>
                <div class='definition-label'>Period</div>
                <div class='definition-value'>${escapeHtml(budget.period || 'N/A')}</div>
            </div>
        </div>
        ${periodLabel ? "<p class='field-hint' style='margin-top:.5rem'>" + escapeHtml(periodLabel) + "</p>" : ''}
    </div>`;
        }

        html += '</div>';
        return html;
    }

    function renderAgentTriggersTab(detail, disabledAttr){
        var triggers = detail.triggers || [];
        var restrictions = detail.restrictions || [];
        return `
<div class='agent-config-form'>
    <div class='field'>
        <span>Triggers (one per line)</span>
        <textarea id='acfTriggers' class='control' rows='6' placeholder='Enter trigger phrases, one per line'${disabledAttr}>${escapeHtml(triggers.join('\n'))}</textarea>
    </div>
    <div class='field'>
        <span>Restrictions (one per line)</span>
        <textarea id='acfRestrictions' class='control' rows='6' placeholder='Enter restrictions, one per line'${disabledAttr}>${escapeHtml(restrictions.join('\n'))}</textarea>
    </div>
</div>`;
    }

    function saveAgentConfig(agentName){
        var state = dashboardState.agents;
        if(state.configSaving) return;

        var body = {};

        // LLM Config
        var llmProvider = (document.getElementById('acfLlmProvider') || {}).value || '';
        var llmModel = (document.getElementById('acfLlmModel') || {}).value || '';
        var llmTemp = (document.getElementById('acfLlmTemp') || {}).value;
        var llmMaxTokens = (document.getElementById('acfLlmMaxTokens') || {}).value;
        body.llmConfig = {};
        if(llmProvider) body.llmConfig.provider = llmProvider;
        if(llmModel) body.llmConfig.model = llmModel;
        if(llmTemp !== '' && llmTemp != null) body.llmConfig.temperature = parseFloat(llmTemp);
        if(llmMaxTokens !== '' && llmMaxTokens != null) body.llmConfig.maxTokens = parseInt(llmMaxTokens, 10) || null;

        // Fallback LLM Config
        var fbProvider = (document.getElementById('acfFbProvider') || {}).value || '';
        var fbModel = (document.getElementById('acfFbModel') || {}).value || '';
        var fbTemp = (document.getElementById('acfFbTemp') || {}).value;
        var fbMaxTokens = (document.getElementById('acfFbMaxTokens') || {}).value;
        if(fbProvider || fbModel){
            body.llmConfig.fallback = {};
            if(fbProvider) body.llmConfig.fallback.provider = fbProvider;
            if(fbModel) body.llmConfig.fallback.model = fbModel;
            if(fbTemp !== '' && fbTemp != null) body.llmConfig.fallback.temperature = parseFloat(fbTemp);
            if(fbMaxTokens !== '' && fbMaxTokens != null) body.llmConfig.fallback.maxTokens = parseInt(fbMaxTokens, 10) || null;
        }

        // Permissions
        body.permissions = {
            internet: !!(document.getElementById('acfPerm_internet') || {}).checked,
            fileSystem: !!(document.getElementById('acfPerm_fileSystem') || {}).checked,
            codeExecution: !!(document.getElementById('acfPerm_codeExecution') || {}).checked,
            databaseAccess: !!(document.getElementById('acfPerm_databaseAccess') || {}).checked
        };

        // Token Budget
        var budgetMax = parseInt((document.getElementById('acfBudgetMax') || {}).value, 10);
        var budgetPeriod = (document.getElementById('acfBudgetPeriod') || {}).value || 'Session';
        var budgetAction = (document.getElementById('acfBudgetAction') || {}).value || 'Stop';
        var budgetPauseMins = parseInt((document.getElementById('acfBudgetPauseMins') || {}).value, 10);
        body.tokenBudget = {
            maxTokensPerPeriod: isNaN(budgetMax) ? 0 : budgetMax,
            period: budgetPeriod,
            actionOnExceeded: budgetAction,
            pauseResumeMinutes: isNaN(budgetPauseMins) ? 5 : budgetPauseMins
        };

        // Triggers
        var triggersText = (document.getElementById('acfTriggers') || {}).value || '';
        body.triggers = triggersText.split('\n').map(function(s){ return s.trim(); }).filter(function(s){ return s.length > 0; });

        // Restrictions
        var restrictionsText = (document.getElementById('acfRestrictions') || {}).value || '';
        body.restrictions = restrictionsText.split('\n').map(function(s){ return s.trim(); }).filter(function(s){ return s.length > 0; });

        // Priority
        var priorityVal = parseInt((document.getElementById('acfPriority') || {}).value, 10);
        body.priority = isNaN(priorityVal) ? 0 : priorityVal;

        state.configSaving = true;
        state.configError = '';
        state.configMessage = '';
        renderAgentConfigCard();

        apiRequest('/api/agents/' + encodeURIComponent(agentName) + '/config', {
            method:'PUT',
            body:JSON.stringify(body)
        }).then(function(){
            state.configMessage = 'Agent configuration saved successfully.';
            state.items = null;
            return loadAgentConfig(agentName);
        }).catch(function(error){
            state.configError = error.message;
        }).finally(function(){
            state.configSaving = false;
            renderAgentConfigCard();
        });
    }

    function bindAgentConfigInteractions(){
        var tempSlider = document.getElementById('acfLlmTemp');
        var tempVal = document.getElementById('acfLlmTempVal');
        if(tempSlider && tempVal){
            tempSlider.oninput = function(){ tempVal.textContent = tempSlider.value; };
        }
        var fbTempSlider = document.getElementById('acfFbTemp');
        var fbTempVal = document.getElementById('acfFbTempVal');
        if(fbTempSlider && fbTempVal){
            fbTempSlider.oninput = function(){ fbTempVal.textContent = fbTempSlider.value; };
        }
        var actionSelect = document.getElementById('acfBudgetAction');
        var pauseField = document.getElementById('acfPauseMinsField');
        if(actionSelect && pauseField){
            actionSelect.onchange = function(){
                pauseField.style.display = actionSelect.value === 'PauseResume' ? '' : 'none';
            };
        }
    }

    // Orchestrator settings.
    function loadOrchestratorConfig(){
        var state = dashboardState.agents;
        state.orchLoading = true;
        renderOrchestratorSettings();

        apiRequest('/api/orchestrator/config').then(function(config){
            state.orchConfig = config;
        }).catch(function(error){
            state.orchError = error.message;
        }).finally(function(){
            state.orchLoading = false;
            renderOrchestratorSettings();
        });
    }

    function renderOrchestratorSettings(){
        var host = document.getElementById('orchSettingsSection');
        if(!host) return;
        var state = dashboardState.agents;

        if(state.orchLoading && !state.orchConfig){
            host.innerHTML = '';
            return;
        }
        if(!state.orchConfig){
            host.innerHTML = '';
            return;
        }

        var config = state.orchConfig;
        var vis = config.delegationVisibility || 'Invisible';
        var isSaving = state.orchSaving;
        var disabledAttr = isSaving ? ' disabled' : '';
        var visOptions = '';
        var visValues = ['Invisible','Visible','Detailed'];
        for(var i=0;i<visValues.length;i++){
            visOptions += "<option value='" + visValues[i] + "'" + (visValues[i] === vis ? ' selected' : '') + ">" + visValues[i] + "</option>";
        }

        var banner = '';
        if(state.orchError) banner = renderStatusBanner(state.orchError, 'error');
        else if(state.orchMessage) banner = renderStatusBanner(state.orchMessage, 'success');

        host.innerHTML = `
<div class='card catalog-card orch-settings'>
    <div class='section-heading'>
        <div>
            <h2>\uD83C\uDFAF Orchestrator Settings</h2>
            <p>Configure how agents are delegated and coordinated.</p>
        </div>
    </div>
    ${banner}
    <div class='orch-bar'>
        <div class='field'>
            <span>Default Agent</span>
            <input type='text' class='control' value='${escapeHtml(config.defaultAgent || 'N/A')}' disabled />
        </div>
        <div class='field'>
            <span>Delegation Visibility</span>
            <select id='orchVisSelect' class='control'${disabledAttr}>${visOptions}</select>
        </div>
        <button type='button' class='primary-button' id='orchSaveBtn' style='min-height:3rem;align-self:flex-end'${disabledAttr}>${isSaving ? 'Saving\u2026' : 'Save'}</button>
    </div>
</div>`;

        var saveBtn = document.getElementById('orchSaveBtn');
        if(saveBtn){
            saveBtn.onclick = saveOrchestratorConfig;
        }
    }

    function saveOrchestratorConfig(){
        var state = dashboardState.agents;
        if(state.orchSaving) return;
        var vis = (document.getElementById('orchVisSelect') || {}).value || 'Invisible';

        state.orchSaving = true;
        state.orchError = '';
        state.orchMessage = '';
        renderOrchestratorSettings();

        apiRequest('/api/orchestrator/config', {
            method:'PUT',
            body:JSON.stringify({delegationVisibility:vis})
        }).then(function(){
            state.orchMessage = 'Orchestrator settings saved.';
            if(state.orchConfig) state.orchConfig.delegationVisibility = vis;
        }).catch(function(error){
            state.orchError = error.message;
        }).finally(function(){
            state.orchSaving = false;
            renderOrchestratorSettings();
        });
    }

    // Skills page.
    function renderSkillsPage(){
        queuePageInit(initSkillsPage);
        return `
<!-- Page: Skills -->
<section class='page-shell'>
    <header class='page-header'>
        <div>
            <h1>Skills</h1>
            <p>Browse installed skills, inspect version and type metadata, and expand cards to review inputs, outputs, and permissions.</p>
        </div>
        <span class='pill'>Skill catalog</span>
    </header>
    <div class='catalog-list' id='skillsList'>${renderLoadingPanel('Loading skills…')}</div>
</section>`;
    }

    function initSkillsPage(){
        renderSkillsList();
        if(!dashboardState.skills.items){
            loadSkills(false);
        }
    }

    function loadSkills(force){
        var state = dashboardState.skills;
        if(state.isLoading){
            return Promise.resolve();
        }
        if(!force && state.items){
            renderSkillsList();
            return Promise.resolve();
        }

        state.isLoading = true;
        state.error = '';
        renderSkillsList();

        return apiRequest('/api/skills').then(function(items){
            state.items = items || [];
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isLoading = false;
            renderSkillsList();
        });
    }

    function loadSkillDetail(name){
        var state = dashboardState.skills;
        if(state.detailLoading[name]){
            return Promise.resolve();
        }

        state.detailLoading[name] = true;
        state.detailError[name] = '';
        renderSkillsList();

        return apiRequest('/api/skills/' + encodeURIComponent(name)).then(function(detail){
            state.detailByName[name] = detail;
        }).catch(function(error){
            state.detailError[name] = error.message;
        }).finally(function(){
            state.detailLoading[name] = false;
            renderSkillsList();
        });
    }

    function toggleSkillExpansion(name){
        var state = dashboardState.skills;
        state.expandedName = state.expandedName === name ? '' : name;
        renderSkillsList();
        if(state.expandedName === name && !state.detailByName[name]){
            loadSkillDetail(name);
        }
    }

    function formatDefaultValue(value){
        if(value === null || value === undefined || value === ''){
            return 'None';
        }
        try{
            var serialized = typeof value === 'string' ? value : JSON.stringify(value);
            return serialized == null ? 'None' : serialized;
        } catch(error){
            return String(value);
        }
    }

    function renderSkillInputs(inputs){
        if(!inputs || !inputs.length){
            return `<div class='detail-card'><h3>Inputs</h3><p>No inputs declared.</p></div>`;
        }

        var html = '';
        for(var i=0;i<inputs.length;i++){
            var input = inputs[i];
            html += `
<div class='detail-card'>
    <h3>${escapeHtml(input.name || 'Unnamed input')}</h3>
    <p>${escapeHtml(input.description || 'No description provided.')}</p>
    <div class='badge-row'>
        <span class='badge is-muted'>${escapeHtml(input.type || 'unknown')}</span>
        <span class='badge is-accent'>${input.required ? 'Required' : 'Optional'}</span>
        <span class='badge is-muted'>Default: ${escapeHtml(formatDefaultValue(input.defaultValue))}</span>
    </div>
</div>`;
        }
        return html;
    }

    function renderSkillOutputs(outputs){
        if(!outputs || !outputs.length){
            return `<div class='detail-card'><h3>Outputs</h3><p>No outputs declared.</p></div>`;
        }

        var html = '';
        for(var i=0;i<outputs.length;i++){
            var output = outputs[i];
            html += `
<div class='detail-card'>
    <h3>${escapeHtml(output.name || 'Unnamed output')}</h3>
    <p>${escapeHtml(output.description || 'No description provided.')}</p>
    <div class='badge-row'>
        <span class='badge is-muted'>${escapeHtml(output.type || 'unknown')}</span>
    </div>
</div>`;
        }
        return html;
    }

    function renderSkillPermissions(permissions){
        if(!permissions){
            return `<div class='detail-card'><h3>Permissions</h3><p>No explicit permissions required.</p></div>`;
        }

        return `
<div class='detail-card'>
    <h3>Allowed tools</h3>
    ${renderTokens(permissions.allowedTools || [], 'No tool restrictions declared.')}
</div>
<div class='detail-card'>
    <h3>Required capabilities</h3>
    ${renderTokens(permissions.requiredCapabilities || [], 'No extra runtime capabilities declared.')}
</div>`;
    }

    function renderSkillDetail(summary, detail, isLoading, error){
        if(isLoading){
            return `<div class='expand-panel'>${renderLoadingPanel('Loading skill details…')}</div>`;
        }
        if(error){
            return `<div class='expand-panel'>${renderStatusBanner(error, 'error')}<div class='settings-actions'><button type='button' class='secondary-button' data-skill-detail-retry='${escapeHtml(summary.name)}'>Retry</button></div></div>`;
        }
        if(!detail){
            return '';
        }

        return `
<div class='expand-panel'>
    <div class='detail-columns'>
        ${renderSkillInputs(detail.inputs || [])}
    </div>
    <div class='detail-columns'>
        ${renderSkillOutputs(detail.outputs || [])}
    </div>
    <div class='detail-columns'>
        ${renderSkillPermissions(detail.permissions)}
    </div>
</div>`;
    }

    function renderSkillsList(){
        var container = document.getElementById('skillsList');
        if(!container){
            return;
        }

        var state = dashboardState.skills;
        var html = '';
        if(state.error){
            html += renderStatusBanner(state.error, 'error');
        }

        if(state.isLoading && !state.items){
            container.innerHTML = html + renderLoadingPanel('Loading skills…');
            container.onclick = handleSkillsClick;
            return;
        }

        if(!state.items || state.items.length === 0){
            container.innerHTML = html + renderSettingsEmptyState(
                'No skills installed.',
                'Add SKILL.md files to the skills directory.',
                'Reload skills',
                "data-skills-action='reload'");
            container.onclick = handleSkillsClick;
            return;
        }

        for(var i=0;i<state.items.length;i++){
            var summary = state.items[i];
            var expanded = state.expandedName === summary.name;
            html += `
<article class='card catalog-card'>
    <button type='button' class='catalog-trigger' data-skill-name='${escapeHtml(summary.name)}' aria-expanded='${expanded ? 'true' : 'false'}'>
        <div class='catalog-summary'>
            <div>
                <h2 class='catalog-title'>${escapeHtml(summary.name)}</h2>
                <p class='catalog-description'>${escapeHtml(summary.description || 'No description provided.')}</p>
                <div class='badge-row'>
                    <span class='badge is-accent'>${escapeHtml(getSkillTypeLabel(summary.type))}</span>
                    <span class='badge is-muted'>v${escapeHtml(summary.version || '0.0.0')}</span>
                </div>
            </div>
            <span class='expand-icon' aria-hidden='true'>${expanded ? '−' : '+'}</span>
        </div>
    </button>
    ${expanded ? renderSkillDetail(summary, state.detailByName[summary.name], !!state.detailLoading[summary.name], state.detailError[summary.name]) : ''}
</article>`;
        }

        container.innerHTML = html;
        container.onclick = handleSkillsClick;
    }

    function handleSkillsClick(event){
        var reloadButton = event.target.closest('[data-skills-action="reload"]');
        if(reloadButton){
            loadSkills(true);
            return;
        }

        var retryButton = event.target.closest('[data-skill-detail-retry]');
        if(retryButton){
            loadSkillDetail(retryButton.getAttribute('data-skill-detail-retry'));
            return;
        }

        var trigger = event.target.closest('[data-skill-name]');
        if(trigger){
            toggleSkillExpansion(trigger.getAttribute('data-skill-name'));
        }
    }

    // Channels page.
    function getActiveChannelId(){
        var hash = window.location.hash;
        var prefix = '#/channels/';
        if(hash.indexOf(prefix) === 0){
            return hash.substring(prefix.length).split('/')[0];
        }
        return '';
    }

    function renderChannelsPage(){
        var channelId = getActiveChannelId();
        if(channelId){
            queuePageInit(function(){ initChannelDetailPage(channelId); });
            return renderChannelDetailPage(channelId);
        }
        queuePageInit(initChannelsOverview);
        return `
<!-- Page: Channels -->
<section class='page-shell'>
    <header class='page-header'>
        <div>
            <h1>Channels</h1>
            <p>Configure external messaging connectors such as Telegram, Discord, Email, and WhatsApp.</p>
        </div>
        <span class='pill'>Connectors</span>
    </header>
    <div id='channelsStatus'></div>
    <div class='settings-grid' id='channelsGrid'>${renderLoadingPanel('Loading channels\u2026')}</div>
</section>`;
    }

    function initChannelsOverview(){
        renderChannelsGrid();
        if(!dashboardState.channels.items){
            loadChannels(false);
        }
    }

    function loadChannels(force){
        var state = dashboardState.channels;
        if(state.isLoading){
            return Promise.resolve();
        }
        if(!force && state.items){
            renderChannelsGrid();
            return Promise.resolve();
        }
        state.isLoading = true;
        state.error = '';
        renderChannelsGrid();

        return apiRequest('/api/channels').then(function(payload){
            state.items = payload && payload.channels ? payload.channels : [];
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isLoading = false;
            renderChannelsGrid();
        });
    }

    function renderChannelsGrid(){
        var host = document.getElementById('channelsGrid');
        var statusHost = document.getElementById('channelsStatus');
        if(!host) return;
        var state = dashboardState.channels;
        if(statusHost){
            statusHost.innerHTML = renderStatusBanner(state.error, 'error') || renderStatusBanner(state.message, 'success');
        }
        if(state.isLoading && !state.items){
            host.innerHTML = renderLoadingPanel('Loading channels\u2026');
            return;
        }
        if(!state.items || state.items.length === 0){
            host.innerHTML = renderSettingsEmptyState('No channels found', 'Channel definitions could not be loaded.');
            return;
        }
        var html = '';
        for(var i = 0; i < state.items.length; i++){
            var ch = state.items[i];
            var statusClass = ch.enabled ? 'is-accent' : (ch.configured ? 'is-muted' : 'is-muted');
            var statusLabel = ch.enabled ? 'Enabled' : (ch.configured ? 'Disabled' : 'Not Configured');
            html += `
<a href='#/channels/${escapeHtml(ch.id)}' class='card catalog-card' style='text-decoration:none;color:inherit;cursor:pointer'>
    <div class='catalog-summary'>
        <div>
            <div class='catalog-title'><span style='margin-right:.5rem;font-size:1.3rem'>${escapeHtml(ch.icon)}</span>${escapeHtml(ch.name)}</div>
            <div class='catalog-description'>${escapeHtml(ch.description)}</div>
        </div>
        <span class='badge ${statusClass}'>${statusLabel}</span>
    </div>
</a>`;
        }
        host.innerHTML = html;
    }

    function renderChannelDetailPage(channelId){
        return `
<section class='page-shell'>
    <header class='page-header'>
        <div>
            <h1 id='channelDetailTitle'>Channel Configuration</h1>
            <p id='channelDetailDesc'>Loading channel details\u2026</p>
        </div>
        <a href='#/channels' class='secondary-button'>\u2190 Back to Channels</a>
    </header>
    <div id='channelDetailStatus'></div>
    <section class='card settings-card' id='channelDetailCard'>${renderLoadingPanel('Loading channel configuration\u2026')}</section>
</section>`;
    }

    function initChannelDetailPage(channelId){
        var state = dashboardState.channels;
        state.detailId = channelId;
        state.detail = null;
        state.message = '';
        state.error = '';
        loadChannelDetail(channelId);
    }

    function loadChannelDetail(channelId){
        var state = dashboardState.channels;
        state.isLoading = true;
        state.error = '';
        renderChannelDetailCard();

        return apiRequest('/api/channels/' + encodeURIComponent(channelId)).then(function(detail){
            state.detail = detail;
            var titleEl = document.getElementById('channelDetailTitle');
            var descEl = document.getElementById('channelDetailDesc');
            if(titleEl) titleEl.textContent = (detail.icon || '') + ' ' + (detail.name || channelId);
            if(descEl) descEl.textContent = detail.description || '';
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isLoading = false;
            renderChannelDetailCard();
        });
    }

    function renderChannelDetailCard(){
        var card = document.getElementById('channelDetailCard');
        var statusHost = document.getElementById('channelDetailStatus');
        if(!card) return;
        var state = dashboardState.channels;
        if(statusHost){
            statusHost.innerHTML = renderStatusBanner(state.error, 'error') || renderStatusBanner(state.message, 'success');
        }
        if(state.isLoading && !state.detail){
            card.innerHTML = renderLoadingPanel('Loading channel configuration\u2026');
            return;
        }
        if(!state.detail){
            card.innerHTML = renderSettingsEmptyState('Channel not found', 'Could not load configuration for this channel.');
            return;
        }
        var detail = state.detail;
        var channelId = detail.id || state.detailId;
        card.innerHTML = renderChannelForm(channelId, detail);
    }

    function renderChannelForm(channelId, detail){
        var isSaving = dashboardState.channels.isSaving;
        var disabledAttr = isSaving ? 'disabled' : '';
        var enabled = !!detail.enabled;
        var fields = detail.fields || {};
        var html = `
<div class='section-heading'>
    <div>
        <h2>${escapeHtml(detail.name)} Configuration</h2>
        <p>Update settings for this channel connector. Sensitive fields are masked for security.</p>
    </div>
</div>
<div class='field'>
    <span>Status</span>
    <select id='channelEnabledSelect' class='control' ${disabledAttr}>
        <option value='true'${enabled ? " selected" : ""}>Enabled</option>
        <option value='false'${!enabled ? " selected" : ""}>Disabled</option>
    </select>
</div>`;

        if(channelId === 'telegram'){
            html += renderChannelField('channelBotToken', 'Bot Token', fields.botToken || '', 'password', 'Telegram bot token from @BotFather', disabledAttr);
            html += renderChannelField('channelWebhookUrl', 'Webhook URL (optional)', fields.webhookUrl || '', 'url', 'https://example.com/webhook', disabledAttr);
        } else if(channelId === 'discord'){
            html += renderChannelField('channelBotToken', 'Bot Token', fields.botToken || '', 'password', 'Discord bot token', disabledAttr);
            html += renderChannelField('channelGuildId', 'Guild ID', fields.guildId || '', 'text', 'Discord server (guild) ID', disabledAttr);
        } else if(channelId === 'email'){
            html += renderChannelField('channelSmtpHost', 'SMTP Host', fields.smtpHost || '', 'text', 'smtp.example.com', disabledAttr);
            html += renderChannelField('channelSmtpPort', 'SMTP Port', fields.smtpPort || '587', 'number', '587', disabledAttr);
            html += renderChannelField('channelUsername', 'Username', fields.username || '', 'text', 'user@example.com', disabledAttr);
            html += renderChannelField('channelPassword', 'Password', fields.password || '', 'password', 'SMTP password', disabledAttr);
            html += renderChannelField('channelFromAddress', 'From Address', fields.fromAddress || '', 'email', 'noreply@example.com', disabledAttr);
        } else if(channelId === 'whatsapp'){
            html += `<div class='status-banner is-info' style='background:rgba(80,250,123,.06);border-color:rgba(80,250,123,.18);margin-top:.5rem'><p>Coming soon \u2014 WhatsApp Business API integration is planned for a future release.</p></div>`;
        }

        html += `
<div class='settings-actions' style='margin-top:1rem'>
    <a href='#/channels' class='secondary-button'>Cancel</a>
    <button type='button' class='primary-button' id='channelSaveButton' ${disabledAttr}>${isSaving ? 'Saving\u2026' : 'Save settings'}</button>
</div>`;
        return html;
    }

    function renderChannelField(id, label, value, type, placeholder, disabledAttr){
        var inputValue = value === '\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022' ? '' : value;
        return `
<div class='field'>
    <span>${escapeHtml(label)}</span>
    <input id='${escapeHtml(id)}' class='control' type='${type}' value='${escapeHtml(inputValue)}' placeholder='${escapeHtml(placeholder)}' ${disabledAttr}>
</div>`;
    }

    function saveChannelConfig(){
        var state = dashboardState.channels;
        if(state.isSaving || !state.detail) return;
        var channelId = state.detail.id || state.detailId;

        var body = {};
        var enabledSelect = document.getElementById('channelEnabledSelect');
        if(enabledSelect){
            body.enabled = enabledSelect.value === 'true';
        }

        if(channelId === 'telegram'){
            collectField(body, 'channelBotToken', 'botToken');
            collectField(body, 'channelWebhookUrl', 'webhookUrl');
        } else if(channelId === 'discord'){
            collectField(body, 'channelBotToken', 'botToken');
            collectField(body, 'channelGuildId', 'guildId');
        } else if(channelId === 'email'){
            collectField(body, 'channelSmtpHost', 'smtpHost');
            collectFieldAsInt(body, 'channelSmtpPort', 'smtpPort');
            collectField(body, 'channelUsername', 'username');
            collectField(body, 'channelPassword', 'password');
            collectField(body, 'channelFromAddress', 'fromAddress');
        } else if(channelId === 'whatsapp'){
            // No additional fields.
        }

        state.isSaving = true;
        state.error = '';
        state.message = '';
        renderChannelDetailCard();

        apiRequest('/api/channels/' + encodeURIComponent(channelId), {
            method:'PUT',
            body:JSON.stringify(body)
        }).then(function(result){
            state.message = (result && result.message) || 'Channel configuration saved.';
            state.items = null;
            return loadChannelDetail(channelId);
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isSaving = false;
            renderChannelDetailCard();
        });
    }

    function collectField(body, elementId, fieldName){
        var el = document.getElementById(elementId);
        if(el && el.value.trim()){
            body[fieldName] = el.value.trim();
        }
    }

    function collectFieldAsInt(body, elementId, fieldName){
        var el = document.getElementById(elementId);
        if(el && el.value.trim()){
            var parsed = parseInt(el.value.trim(), 10);
            if(!isNaN(parsed)){
                body[fieldName] = parsed;
            }
        }
    }

    // Channel page click delegation.
    pageContainer.addEventListener('click', function(event){
        if(event.target.closest('#channelSaveButton')){
            event.preventDefault();
            saveChannelConfig();
        }
    });

    // Timers page.
    var timerCronPresets = [
        {label:'Select a preset…',value:''},
        {label:'Every minute',value:'* * * * *'},
        {label:'Every 5 minutes',value:'*/5 * * * *'},
        {label:'Every hour',value:'0 * * * *'},
        {label:'Every day at 9am',value:'0 9 * * *'},
        {label:'Every Monday at 9am',value:'0 9 * * 1'},
        {label:'Every month 1st at midnight',value:'0 0 1 * *'}
    ];

    function renderTimersPage(){
        queuePageInit(initTimersPage);
        return `
<!-- Page: Timers -->
<section class="page-shell">
    <header class="page-header">
        <div>
            <h1>Timers</h1>
            <p>Manage scheduled tasks and automated prompts that run on a recurring or one-time basis.</p>
        </div>
        <span class="pill">Scheduler</span>
    </header>
    <div id="timersStatus"></div>
    <div class="timers-toolbar">
        <button type="button" class="primary-button" data-timers-action="add">+ Add Timer</button>
        <button type="button" class="secondary-button" data-timers-action="reload">Refresh</button>
    </div>
    <div id="timersList">${renderLoadingPanel('Loading scheduled tasks…')}</div>
    <div id="timerModalHost"></div>
</section>`;
    }

    function initTimersPage(){
        renderTimersList();
        loadTimers(false);
        var host = document.getElementById('pageContent');
        if(host && !host._timersClickBound){
            host._timersClickBound = true;
            host.addEventListener('click', handleTimersClick);
            host.addEventListener('change', handleTimersChange);
        }
    }

    function loadTimers(force){
        var state = dashboardState.timers;
        if(state.loading) return Promise.resolve();
        if(!force && state.tasks && state.tasks.length > 0){
            renderTimersList();
            return Promise.resolve();
        }
        state.loading = true;
        renderTimersList();
        return apiRequest('/api/scheduled-tasks').then(function(items){
            state.tasks = items || [];
        }).catch(function(err){
            state.tasks = [];
            renderTimersStatus(err.message, 'error');
        }).finally(function(){
            state.loading = false;
            renderTimersList();
        });
    }

    function renderTimersStatus(message, kind){
        var host = document.getElementById('timersStatus');
        if(host){
            host.innerHTML = renderStatusBanner(message, kind);
        }
    }

    function renderTimersList(){
        var host = document.getElementById('timersList');
        if(!host) return;
        var state = dashboardState.timers;
        if(state.loading && state.tasks.length === 0){
            host.innerHTML = renderLoadingPanel('Loading scheduled tasks…');
            return;
        }
        if(!state.tasks || state.tasks.length === 0){
            host.innerHTML = renderSettingsEmptyState(
                'No timers configured',
                'Create a scheduled task to automate prompts on a recurring or one-time schedule.',
                'Add Timer',
                'data-timers-action="add"'
            );
            return;
        }
        var rows = '';
        for(var i = 0; i < state.tasks.length; i++){
            var t = state.tasks[i];
            var statusBadge = t.enabled
                ? '<span class="timer-badge is-enabled">Enabled</span>'
                : '<span class="timer-badge is-disabled">Disabled</span>';
            var lastRunBadge = '';
            if(t.lastRunStatus === 'Failed'){
                lastRunBadge = ' <span class="timer-badge is-failed">Failed</span>';
            } else if(t.lastRunStatus === 'Running'){
                lastRunBadge = ' <span class="timer-badge is-running">Running</span>';
            } else if(t.lastRunStatus === 'Success'){
                lastRunBadge = ' <span class="timer-badge is-success">OK</span>';
            }
            var schedule = t.isRecurring
                ? escapeHtml(describeCron(t.cronExpression)) + ' <code style="font-size:.75rem;color:var(--text-secondary)">' + escapeHtml(t.cronExpression || '') + '</code>'
                : (t.scheduledTime ? 'Once at ' + escapeHtml(formatTimerDate(t.scheduledTime)) : 'One-time');
            rows += '<tr>';
            rows += '<td><div class="timer-name">' + escapeHtml(t.name) + '</div>';
            if(t.description){
                rows += '<div class="timer-description">' + escapeHtml(t.description) + '</div>';
            }
            rows += '</td>';
            rows += '<td>' + schedule + '</td>';
            rows += '<td>' + escapeHtml(t.agentName || 'Default') + '</td>';
            rows += '<td>' + statusBadge + '</td>';
            rows += '<td>' + (t.lastRunAt ? escapeHtml(formatTimerDate(t.lastRunAt)) : '—') + lastRunBadge + '</td>';
            rows += '<td>' + (t.nextRunAt ? escapeHtml(formatTimerDate(t.nextRunAt)) : '—') + '</td>';
            rows += '<td>' + (t.runCount != null ? t.runCount : 0) + '</td>';
            rows += '<td><div class="timer-actions">';
            rows += '<button type="button" class="is-run" data-timer-run="' + escapeHtml(t.id) + '" title="Run now">&#9654;</button>';
            rows += '<button type="button" data-timer-edit="' + escapeHtml(t.id) + '" title="Edit">&#9998;</button>';
            rows += '<button type="button" class="is-danger" data-timer-delete="' + escapeHtml(t.id) + '" title="Delete">&#128465;</button>';
            rows += '<div class="toggle-switch" style="margin-left:.3rem">';
            rows += '<input type="checkbox"' + (t.enabled ? ' checked' : '') + ' data-timer-toggle="' + escapeHtml(t.id) + '">';
            rows += '<span class="toggle-slider"></span>';
            rows += '</div>';
            rows += '</div></td>';
            rows += '</tr>';
        }
        host.innerHTML = '<div class="timers-table-wrap"><table class="timers-table">'
            + '<thead><tr><th>Name</th><th>Schedule</th><th>Agent</th><th>Status</th><th>Last Run</th><th>Next Run</th><th>Runs</th><th>Actions</th></tr></thead>'
            + '<tbody>' + rows + '</tbody>'
            + '</table></div>';
    }

    function describeCron(expr){
        if(!expr) return '';
        var presets = {
            '* * * * *':'Every minute',
            '*/5 * * * *':'Every 5 minutes',
            '*/15 * * * *':'Every 15 minutes',
            '*/30 * * * *':'Every 30 minutes',
            '0 * * * *':'Every hour',
            '0 */2 * * *':'Every 2 hours',
            '0 */6 * * *':'Every 6 hours',
            '0 */12 * * *':'Every 12 hours',
            '0 0 * * *':'Every day at midnight',
            '0 9 * * *':'Every day at 9am',
            '0 9 * * 1':'Every Monday at 9am',
            '0 9 * * 1-5':'Weekdays at 9am',
            '0 0 1 * *':'Monthly on the 1st',
            '0 0 * * 0':'Every Sunday at midnight'
        };
        return presets[expr] || expr;
    }

    function formatTimerDate(isoStr){
        if(!isoStr) return '';
        try{
            var d = new Date(isoStr);
            return d.toLocaleString();
        } catch(e){
            return isoStr;
        }
    }

    function handleTimersClick(event){
        var addBtn = event.target.closest('[data-timers-action="add"]');
        if(addBtn){
            openTimerModal(null);
            return;
        }
        var reloadBtn = event.target.closest('[data-timers-action="reload"]');
        if(reloadBtn){
            loadTimers(true);
            return;
        }
        var editBtn = event.target.closest('[data-timer-edit]');
        if(editBtn){
            var editId = editBtn.getAttribute('data-timer-edit');
            var task = findTimerById(editId);
            if(task) openTimerModal(task);
            return;
        }
        var deleteBtn = event.target.closest('[data-timer-delete]');
        if(deleteBtn){
            var delId = deleteBtn.getAttribute('data-timer-delete');
            var delTask = findTimerById(delId);
            var taskName = delTask ? delTask.name : delId;
            if(window.confirm('Delete timer "' + taskName + '"? This cannot be undone.')){
                deleteTimer(delId);
            }
            return;
        }
        var runBtn = event.target.closest('[data-timer-run]');
        if(runBtn){
            runTimerNow(runBtn.getAttribute('data-timer-run'));
            return;
        }
        var modalSave = event.target.closest('[data-timer-modal="save"]');
        if(modalSave){
            saveTimerFromModal();
            return;
        }
        var modalCancel = event.target.closest('[data-timer-modal="cancel"]');
        if(modalCancel){
            closeTimerModal();
            return;
        }
        var presetSelect = event.target.closest('[data-timer-cron-preset]');
        if(presetSelect && presetSelect.value){
            var cronInput = document.getElementById('timerCronExpression');
            if(cronInput) cronInput.value = presetSelect.value;
            return;
        }
        var overlay = event.target.closest('.timer-modal-overlay');
        if(overlay && event.target === overlay){
            closeTimerModal();
            return;
        }
    }

    function handleTimersChange(event){
        var toggle = event.target.closest('[data-timer-toggle]');
        if(toggle){
            var tid = toggle.getAttribute('data-timer-toggle');
            if(toggle.checked){
                enableTimer(tid);
            } else {
                disableTimer(tid);
            }
            return;
        }
        var schedType = event.target.closest('input[name="timerScheduleType"]');
        if(schedType){
            var cronSection = document.getElementById('timerCronSection');
            var onceSection = document.getElementById('timerOnceSection');
            if(cronSection) cronSection.style.display = schedType.value === 'recurring' ? '' : 'none';
            if(onceSection) onceSection.style.display = schedType.value === 'onetime' ? '' : 'none';
            return;
        }
        var presetSel = event.target.closest('[data-timer-cron-preset]');
        if(presetSel && presetSel.value){
            var ci = document.getElementById('timerCronExpression');
            if(ci) ci.value = presetSel.value;
        }
    }

    function findTimerById(id){
        var tasks = dashboardState.timers.tasks;
        for(var i = 0; i < tasks.length; i++){
            if(tasks[i].id === id) return tasks[i];
        }
        return null;
    }

    function openTimerModal(task){
        dashboardState.timers.editingTask = task;
        dashboardState.timers.showModal = true;
        renderTimerModal();
    }

    function closeTimerModal(){
        dashboardState.timers.editingTask = null;
        dashboardState.timers.showModal = false;
        var host = document.getElementById('timerModalHost');
        if(host) host.innerHTML = '';
    }

    function renderTimerModal(){
        var host = document.getElementById('timerModalHost');
        if(!host) return;
        var state = dashboardState.timers;
        if(!state.showModal){
            host.innerHTML = '';
            return;
        }
        var t = state.editingTask;
        var isEdit = t && t.id;
        var title = isEdit ? 'Edit Timer' : 'Add Timer';
        var name = t ? (t.name || '') : '';
        var description = t ? (t.description || '') : '';
        var prompt = t ? (t.prompt || '') : '';
        var isRecurring = t ? (t.isRecurring !== false) : true;
        var cronExpression = t ? (t.cronExpression || '') : '';
        var scheduledTime = t && t.scheduledTime ? t.scheduledTime.substring(0, 16) : '';
        var agentName = t ? (t.agentName || '') : '';
        var tags = t ? (t.tags || '') : '';
        var priority = t ? (t.priority != null ? t.priority : 0) : 0;
        var timeoutSeconds = t ? (t.timeoutSeconds != null ? t.timeoutSeconds : '') : '';
        var maxRetries = t ? (t.maxRetries != null ? t.maxRetries : 0) : 0;
        var enabled = t ? (t.enabled !== false) : true;

        var presetOptions = '';
        for(var p = 0; p < timerCronPresets.length; p++){
            presetOptions += '<option value="' + escapeHtml(timerCronPresets[p].value) + '">' + escapeHtml(timerCronPresets[p].label) + '</option>';
        }

        var html = '<div class="timer-modal-overlay">';
        html += '<div class="timer-modal">';
        html += '<h2>' + escapeHtml(title) + '</h2>';

        html += '<div class="timer-form-group">';
        html += '<label for="timerName">Name *</label>';
        html += '<input type="text" id="timerName" value="' + escapeHtml(name) + '" placeholder="e.g. Daily Summary" required>';
        html += '</div>';

        html += '<div class="timer-form-group">';
        html += '<label for="timerDescription">Description</label>';
        html += '<textarea id="timerDescription" placeholder="Optional description">' + escapeHtml(description) + '</textarea>';
        html += '</div>';

        html += '<div class="timer-form-group">';
        html += '<label for="timerPrompt">Prompt *</label>';
        html += '<textarea id="timerPrompt" rows="3" placeholder="The prompt to send to the agent" required>' + escapeHtml(prompt) + '</textarea>';
        html += '</div>';

        html += '<div class="timer-form-group">';
        html += '<label>Schedule Type</label>';
        html += '<div class="timer-radio-group">';
        html += '<label><input type="radio" name="timerScheduleType" value="recurring"' + (isRecurring ? ' checked' : '') + '> Recurring</label>';
        html += '<label><input type="radio" name="timerScheduleType" value="onetime"' + (!isRecurring ? ' checked' : '') + '> One-time</label>';
        html += '</div>';
        html += '</div>';

        html += '<div id="timerCronSection" style="' + (isRecurring ? '' : 'display:none') + '">';
        html += '<div class="timer-form-group">';
        html += '<label for="timerCronExpression">Cron Expression *</label>';
        html += '<input type="text" id="timerCronExpression" value="' + escapeHtml(cronExpression) + '" placeholder="* * * * *">';
        html += '<div class="timer-cron-presets">';
        html += '<select data-timer-cron-preset>' + presetOptions + '</select>';
        html += '</div>';
        html += '</div>';
        html += '</div>';

        html += '<div id="timerOnceSection" style="' + (!isRecurring ? '' : 'display:none') + '">';
        html += '<div class="timer-form-group">';
        html += '<label for="timerScheduledTime">Date/Time *</label>';
        html += '<input type="datetime-local" id="timerScheduledTime" value="' + escapeHtml(scheduledTime) + '">';
        html += '</div>';
        html += '</div>';

        html += '<div class="timer-form-group">';
        html += '<label for="timerAgent">Agent</label>';
        html += '<select id="timerAgent"><option value="">Default Agent</option></select>';
        html += '</div>';

        html += '<div class="timer-form-group">';
        html += '<label for="timerTags">Tags (comma-separated)</label>';
        html += '<input type="text" id="timerTags" value="' + escapeHtml(tags) + '" placeholder="e.g. daily,report">';
        html += '</div>';

        html += '<div class="timer-form-row">';
        html += '<div class="timer-form-group">';
        html += '<label for="timerPriority">Priority</label>';
        html += '<input type="number" id="timerPriority" value="' + priority + '" min="0">';
        html += '</div>';
        html += '<div class="timer-form-group">';
        html += '<label for="timerTimeout">Timeout (seconds)</label>';
        html += '<input type="number" id="timerTimeout" value="' + escapeHtml(String(timeoutSeconds)) + '" min="0" placeholder="Optional">';
        html += '</div>';
        html += '<div class="timer-form-group">';
        html += '<label for="timerMaxRetries">Max Retries</label>';
        html += '<input type="number" id="timerMaxRetries" value="' + maxRetries + '" min="0">';
        html += '</div>';
        html += '</div>';

        html += '<div class="toggle-row" style="margin-top:.5rem">';
        html += '<label><span>Enabled</span></label>';
        html += '<div class="toggle-switch">';
        html += '<input type="checkbox" id="timerEnabled"' + (enabled ? ' checked' : '') + '>';
        html += '<span class="toggle-slider"></span>';
        html += '</div>';
        html += '</div>';

        html += '<div class="timer-modal-actions">';
        html += '<button type="button" class="secondary-button" data-timer-modal="cancel">Cancel</button>';
        html += '<button type="button" class="primary-button" data-timer-modal="save">' + (isEdit ? 'Save Changes' : 'Create Timer') + '</button>';
        html += '</div>';

        html += '</div></div>';
        host.innerHTML = html;

        loadAgentOptions();
        var agentSelect = document.getElementById('timerAgent');
        if(agentSelect && agentName){
            window.setTimeout(function(){
                agentSelect.value = agentName;
            }, 200);
        }
    }

    function loadAgentOptions(){
        apiRequest('/api/agents').then(function(agents){
            var sel = document.getElementById('timerAgent');
            if(!sel) return;
            var current = sel.value;
            var html = '<option value="">Default Agent</option>';
            if(agents && agents.length){
                for(var i = 0; i < agents.length; i++){
                    var a = agents[i];
                    var aName = a.name || a.agentName || '';
                    html += '<option value="' + escapeHtml(aName) + '">' + escapeHtml(aName) + '</option>';
                }
            }
            sel.innerHTML = html;
            if(current) sel.value = current;
        }).catch(function(){});
    }

    function collectTimerFormData(){
        var nameEl = document.getElementById('timerName');
        var descEl = document.getElementById('timerDescription');
        var promptEl = document.getElementById('timerPrompt');
        var cronEl = document.getElementById('timerCronExpression');
        var timeEl = document.getElementById('timerScheduledTime');
        var agentEl = document.getElementById('timerAgent');
        var tagsEl = document.getElementById('timerTags');
        var priorityEl = document.getElementById('timerPriority');
        var timeoutEl = document.getElementById('timerTimeout');
        var retriesEl = document.getElementById('timerMaxRetries');
        var enabledEl = document.getElementById('timerEnabled');

        var recurringRadio = document.querySelector('input[name="timerScheduleType"]:checked');
        var isRecurring = !recurringRadio || recurringRadio.value === 'recurring';

        var taskName = nameEl ? nameEl.value.trim() : '';
        var taskPrompt = promptEl ? promptEl.value.trim() : '';
        if(!taskName){
            window.alert('Name is required.');
            return null;
        }
        if(!taskPrompt){
            window.alert('Prompt is required.');
            return null;
        }

        var cronVal = cronEl ? cronEl.value.trim() : '';
        var timeVal = timeEl ? timeEl.value : '';
        if(isRecurring && !cronVal){
            window.alert('Cron expression is required for recurring timers.');
            return null;
        }
        if(!isRecurring && !timeVal){
            window.alert('Date/Time is required for one-time timers.');
            return null;
        }

        var timeoutVal = timeoutEl ? timeoutEl.value.trim() : '';

        return {
            name: taskName,
            description: descEl ? descEl.value.trim() : '',
            prompt: taskPrompt,
            isRecurring: isRecurring,
            cronExpression: isRecurring ? cronVal : null,
            scheduledTime: !isRecurring && timeVal ? new Date(timeVal).toISOString() : null,
            agentName: agentEl && agentEl.value ? agentEl.value : null,
            tags: tagsEl ? tagsEl.value.trim() : '',
            priority: priorityEl ? parseInt(priorityEl.value, 10) || 0 : 0,
            timeoutSeconds: timeoutVal ? parseInt(timeoutVal, 10) : null,
            maxRetries: retriesEl ? parseInt(retriesEl.value, 10) || 0 : 0,
            enabled: enabledEl ? enabledEl.checked : true
        };
    }

    function saveTimerFromModal(){
        var data = collectTimerFormData();
        if(!data) return;
        var existing = dashboardState.timers.editingTask;
        var isEdit = existing && existing.id;
        var url = isEdit ? '/api/scheduled-tasks/' + encodeURIComponent(existing.id) : '/api/scheduled-tasks';
        var method = isEdit ? 'PUT' : 'POST';

        apiRequest(url, {
            method: method,
            body: JSON.stringify(data)
        }).then(function(){
            closeTimerModal();
            renderTimersStatus(isEdit ? 'Timer updated.' : 'Timer created.', 'success');
            loadTimers(true);
        }).catch(function(err){
            window.alert('Error: ' + err.message);
        });
    }

    function deleteTimer(id){
        apiRequest('/api/scheduled-tasks/' + encodeURIComponent(id), {method:'DELETE'}).then(function(){
            renderTimersStatus('Timer deleted.', 'success');
            loadTimers(true);
        }).catch(function(err){
            renderTimersStatus('Delete failed: ' + err.message, 'error');
        });
    }

    function enableTimer(id){
        apiRequest('/api/scheduled-tasks/' + encodeURIComponent(id) + '/enable', {method:'POST'}).then(function(){
            loadTimers(true);
        }).catch(function(err){
            renderTimersStatus('Enable failed: ' + err.message, 'error');
        });
    }

    function disableTimer(id){
        apiRequest('/api/scheduled-tasks/' + encodeURIComponent(id) + '/disable', {method:'POST'}).then(function(){
            loadTimers(true);
        }).catch(function(err){
            renderTimersStatus('Disable failed: ' + err.message, 'error');
        });
    }

    function runTimerNow(id){
        apiRequest('/api/scheduled-tasks/' + encodeURIComponent(id) + '/run', {method:'POST'}).then(function(){
            renderTimersStatus('Timer triggered.', 'success');
            loadTimers(true);
        }).catch(function(err){
            renderTimersStatus('Run failed: ' + err.message, 'error');
        });
    }

    // User settings page.
    var userSettingsTabs = [
        { id:'profile', href:'#/user-settings/profile', icon:'👤', label:'Profile' },
        { id:'preferences', href:'#/user-settings/preferences', icon:'⚙️', label:'Assistant Preferences' },
        { id:'tokens', href:'#/user-settings/tokens', icon:'🔑', label:'Tokens' },
        { id:'system', href:'#/user-settings/system', icon:'🔧', label:'System' }
    ];

    function renderUserSettingsPage(){
        queuePageInit(initUserSettingsPage);
        var activeTab = getActiveSettingsTab();
        var subContent = '';
        if(activeTab === 'preferences'){
            subContent = `
        <section class='card settings-card' id='assistantPreferencesSection'>${renderLoadingPanel('Loading assistant preferences…')}</section>
        <section class='card settings-card' id='addPreferenceSection'>${renderLoadingPanel('Loading preference form…')}</section>
        <div class='section-stack' id='preferenceGroups'>${renderLoadingPanel('Loading preferences…')}</div>`;
        } else if(activeTab === 'tokens'){
            subContent = `
        <section class='card settings-card' id='tokenManagementSection'>${renderLoadingPanel('Loading tokens…')}</section>`;
        } else if(activeTab === 'system'){
            subContent = `
        <section class='card settings-card' id='systemSection'></section>`;
        } else {
            subContent = `
        <section class='card settings-card' id='profileSection'>${renderLoadingPanel('Loading profile…')}</section>`;
        }

        return `
<!-- Page: User Settings -->
<section class='page-shell'>
    <header class='page-header'>
        <div>
            <h1>User Settings</h1>
            <p>Maintain your profile, manage API tokens, and add assistant-specific defaults that help OpenVEPA work autonomously.</p>
        </div>
        <span class='pill'>Personalization</span>
    </header>
    <div id='userSettingsStatus'></div>
    ${renderTabBar(userSettingsTabs, activeTab)}
    <div id='settingsContent'>
        ${subContent}
    </div>
</section>`;
    }

    function initUserSettingsPage(){
        var activeTab = getActiveSettingsTab();
        if(activeTab === 'profile' || activeTab === 'preferences'){
            if(!dashboardState.preferences.groups){
                loadPreferences(false);
            }
        }
        if(activeTab === 'tokens'){
            if(!dashboardState.tokens.items){
                loadTokens(false);
            }
        }
        renderUserSettingsSections();
    }

    function loadPreferences(force){
        var state = dashboardState.preferences;
        if(state.isLoading){
            return Promise.resolve();
        }
        if(!force && state.groups){
            renderUserSettingsSections();
            return Promise.resolve();
        }

        state.isLoading = true;
        state.error = '';
        renderUserSettingsSections();

        return apiRequest('/api/preferences').then(function(payload){
            state.groups = normalizePreferencesPayload(payload);
            var nameEntry = findPreferenceEntry(state.groups, 'assistant.name');
            var name = nameEntry ? nameEntry.value : 'VEPA';
            state.assistantName = name;
            applyAssistantName(name);
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isLoading = false;
            renderUserSettingsSections();
        });
    }

    function runPreferenceMutation(work, successMessage){
        var state = dashboardState.preferences;
        if(state.isMutating){
            return;
        }

        state.isMutating = true;
        state.error = '';
        state.message = '';
        renderUserSettingsSections();

        Promise.resolve().then(work).then(function(){
            state.editingKey = '';
            state.message = successMessage;
            return loadPreferences(true);
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isMutating = false;
            renderUserSettingsSections();
        });
    }

    function loadTokens(force){
        var state = dashboardState.tokens;
        if(state.isLoading){
            return Promise.resolve();
        }
        if(!force && state.items){
            renderUserSettingsSections();
            return Promise.resolve();
        }

        state.isLoading = true;
        state.error = '';
        renderUserSettingsSections();

        return apiRequest('/api/tokens').then(function(payload){
            state.items = sortTokenItems(payload && payload.tokens ? payload.tokens : []);
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isLoading = false;
            renderUserSettingsSections();
        });
    }

    function sortTokenItems(tokens){
        return (tokens || []).slice().sort(function(left, right){
            return toSessionTime(right && right.createdAt) - toSessionTime(left && left.createdAt);
        });
    }

    function countActiveTokens(tokens){
        var count = 0;
        for(var i=0;i<(tokens || []).length;i++){
            if(!(tokens[i] && tokens[i].isRevoked)){
                count++;
            }
        }
        return count;
    }

    function getMostRecentlyUsedToken(tokens){
        var activeTokens = (tokens || []).filter(function(token){
            return token && !token.isRevoked;
        });
        if(!activeTokens.length){
            return null;
        }

        activeTokens.sort(function(left, right){
            var leftUsed = toSessionTime(left && left.lastUsedAt);
            var rightUsed = toSessionTime(right && right.lastUsedAt);
            if(rightUsed !== leftUsed){
                return rightUsed - leftUsed;
            }
            return toSessionTime(right && right.createdAt) - toSessionTime(left && left.createdAt);
        });

        return activeTokens[0];
    }

    function getCurrentTokenIndicator(tokens){
        var storedTokenId = getStoredTokenId();
        var items = tokens || [];
        if(storedTokenId){
            for(var i=0;i<items.length;i++){
                if(items[i] && items[i].id === storedTokenId){
                    return {
                        token:items[i],
                        exact:true,
                        html:`Currently using: <strong>${escapeHtml(items[i].name || 'Unnamed token')}</strong> <span class='field-hint'>Matched from this browser's saved token metadata.</span>`
                    };
                }
            }
        }

        var likelyToken = getMostRecentlyUsedToken(items);
        if(!likelyToken){
            return null;
        }

        return {
            token:likelyToken,
            exact:false,
            html:`Currently using: <strong>${escapeHtml(likelyToken.name || 'Unnamed token')}</strong> <span class='field-hint'>Best guess based on the most recent token activity.</span>`
        };
    }

    function renderTokenCreateResult(){
        var result = dashboardState.tokens.createResult;
        if(!result || !result.token){
            return '';
        }

        return `
<div class='token-created-box'>
    <div class='token-created-header'>
        <div>
            <h3>New token created</h3>
            <p class='helper-text'>${escapeHtml(result.name || 'Token')}</p>
        </div>
        <button type='button' class='secondary-button' data-token-action='copy-created'>Copy</button>
    </div>
    <p class='token-warning'>⚠️ Save this token now. It will not be shown again.</p>
    <pre class='detail-code token-secret'>${escapeHtml(result.token)}</pre>
</div>`;
    }

    function renderTokenTable(tokens, currentTokenId){
        if(!tokens || !tokens.length){
            return renderSettingsEmptyState('No tokens yet.', 'Create a token above so this browser, the CLI, or automation can authenticate.', '', '');
        }

        var rows = '';
        for(var i=0;i<tokens.length;i++){
            var token = tokens[i] || {};
            var isCurrent = currentTokenId && token.id === currentTokenId;
            var statusLabel = token.isRevoked ? 'Revoked' : (isCurrent ? 'Active · Current' : 'Active');
            var statusClass = token.isRevoked ? 'is-revoked' : 'is-active';
            var actionLabel = token.isRevoked
                ? 'Revoked'
                : (dashboardState.tokens.revokingId === token.id ? 'Revoking…' : 'Revoke');
            var actionDisabled = token.isRevoked || dashboardState.tokens.revokingId === token.id || dashboardState.tokens.isCreating;
            rows += `
<tr>
    <td>${escapeHtml(token.name || 'Unnamed token')}</td>
    <td><span title='${escapeHtml(formatSessionDateTime(token.createdAt))}'>${escapeHtml(formatSessionDate(token.createdAt))}</span></td>
    <td>${token.lastUsedAt ? `<span title='${escapeHtml(formatSessionDateTime(token.lastUsedAt))}'>${escapeHtml(formatSessionDate(token.lastUsedAt))}</span>` : 'Never'}</td>
    <td><span class='token-status-badge ${statusClass}'>${escapeHtml(statusLabel)}</span></td>
    <td>
        <div class='token-table-actions'>
            <button type='button' class='secondary-button' data-token-action='revoke' data-token-id='${escapeHtmlAttribute(token.id || '')}' ${actionDisabled ? 'disabled' : ''}>${escapeHtml(actionLabel)}</button>
        </div>
    </td>
</tr>`;
        }

        return `
<div class='table-shell'>
    <table class='preference-table'>
        <thead>
            <tr>
                <th>Name</th>
                <th>Created</th>
                <th>Last Used</th>
                <th>Status</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>${rows}
        </tbody>
    </table>
</div>`;
    }

    function renderTokenManagementSection(){
        var state = dashboardState.tokens;
        if(state.isLoading && !state.items){
            return renderLoadingPanel('Loading tokens…');
        }

        var tokens = sortTokenItems(state.items || []);
        var currentIndicator = getCurrentTokenIndicator(tokens);
        var activeCount = countActiveTokens(tokens);
        var revokedCount = Math.max(0, tokens.length - activeCount);
        var currentTokenId = currentIndicator && currentIndicator.exact && currentIndicator.token ? currentIndicator.token.id : '';
        var banner = renderStatusBanner(state.error, 'error') || renderStatusBanner(state.message, 'success');

        return `
<div class='token-management-layout'>
    <div class='section-heading'>
        <div>
            <h2>Token Management</h2>
            <p>Create browser or CLI tokens, review activity, and revoke access when a token is no longer needed.</p>
        </div>
        <div class='settings-actions'>
            <button type='button' class='secondary-button' data-token-action='refresh' ${state.isLoading || state.isCreating || !!state.revokingId ? 'disabled' : ''}>Refresh</button>
        </div>
    </div>
    ${banner}
    ${currentIndicator ? `<div class='status-banner is-info'>${currentIndicator.html}</div>` : ''}
    <div class='token-management-top'>
        <section class='detail-card'>
            <h3>Create new token</h3>
            <form id='tokenCreateForm' class='token-management-form'>
                <label class='field' for='tokenNameInput'>
                    <span>Name</span>
                    <input id='tokenNameInput' class='control' type='text' maxlength='120' placeholder='web-default' ${state.isCreating || !!state.revokingId ? 'disabled' : ''}>
                </label>
                <div class='settings-actions'>
                    <button type='submit' class='primary-button' ${state.isCreating || !!state.revokingId ? 'disabled' : ''}>${state.isCreating ? 'Creating…' : 'Create Token'}</button>
                </div>
                <p class='helper-text'>Use clear names like <span class='inline-code'>web-default</span>, <span class='inline-code'>cli-token</span>, or <span class='inline-code'>ci-runner</span>.</p>
            </form>
            ${renderTokenCreateResult()}
        </section>
        <section class='detail-card'>
            <h3>Token summary</h3>
            <div class='token-summary-grid'>
                <div class='token-summary-card'>
                    <span class='field-hint'>Active</span>
                    <strong>${escapeHtml(formatNumber(activeCount))}</strong>
                </div>
                <div class='token-summary-card'>
                    <span class='field-hint'>Revoked</span>
                    <strong>${escapeHtml(formatNumber(revokedCount))}</strong>
                </div>
                <div class='token-summary-card'>
                    <span class='field-hint'>Total</span>
                    <strong>${escapeHtml(formatNumber(tokens.length))}</strong>
                </div>
            </div>
            <p class='helper-text'>OpenVEPA shows plaintext token values only once. Copy them now and store them securely.</p>
        </section>
    </div>
    ${renderTokenTable(tokens, currentTokenId)}
</div>`;
    }

    function scheduleTokenRevealHide(){
        var state = dashboardState.tokens;
        if(state.revealTimeoutId){
            window.clearTimeout(state.revealTimeoutId);
            state.revealTimeoutId = 0;
        }
        if(!state.createResult){
            return;
        }

        state.revealTimeoutId = window.setTimeout(function(){
            dashboardState.tokens.createResult = null;
            dashboardState.tokens.message = 'Token value hidden after 60 seconds.';
            renderUserSettingsSections();
        }, 60000);
    }

    function createTokenFromSettings(){
        var state = dashboardState.tokens;
        if(state.isCreating){
            return;
        }

        var nameInput = document.getElementById('tokenNameInput');
        var name = nameInput ? nameInput.value.trim() : '';
        if(!name){
            state.error = 'Token name is required.';
            renderUserSettingsSections();
            return;
        }

        state.isCreating = true;
        state.error = '';
        state.message = '';
        renderUserSettingsSections();

        apiRequest('/api/tokens', {
            method:'POST',
            body:JSON.stringify({ name:name })
        }).then(function(result){
            state.createResult = result && result.token ? {
                tokenId:result.tokenId || '',
                token:result.token,
                name:result.name || name
            } : null;
            state.message = 'Token created.';
            scheduleTokenRevealHide();
            return loadTokens(true);
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isCreating = false;
            renderUserSettingsSections();
        });
    }

    function revokeTokenFromSettings(tokenId){
        var state = dashboardState.tokens;
        if(!tokenId || state.revokingId){
            return;
        }

        var tokens = state.items || [];
        var token = null;
        for(var i=0;i<tokens.length;i++){
            if(tokens[i] && tokens[i].id === tokenId){
                token = tokens[i];
                break;
            }
        }
        if(!token || token.isRevoked){
            return;
        }
        if(countActiveTokens(tokens) <= 1){
            state.error = 'You cannot revoke the only active token. Create another token first.';
            renderUserSettingsSections();
            return;
        }
        if(!window.confirm('Revoke token "' + (token.name || token.id || 'token') + '"?')){
            return;
        }

        state.revokingId = tokenId;
        state.error = '';
        state.message = '';
        renderUserSettingsSections();

        apiRequest('/api/tokens/' + encodeURIComponent(tokenId), {
            method:'DELETE'
        }).then(function(){
            if(getStoredTokenId() === tokenId){
                storeTokenId('');
            }
            state.message = 'Token revoked.';
            return loadTokens(true);
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.revokingId = '';
            renderUserSettingsSections();
        });
    }

    function copyCreatedToken(){
        var state = dashboardState.tokens;
        var tokenValue = state.createResult && state.createResult.token ? state.createResult.token : '';
        if(!tokenValue){
            return;
        }

        if(navigator.clipboard && navigator.clipboard.writeText){
            navigator.clipboard.writeText(tokenValue).then(function(){
                showToast('Token copied.');
            }).catch(function(){
                showToast('Unable to copy token.', true);
            });
            return;
        }

        var tempInput = document.createElement('textarea');
        tempInput.value = tokenValue;
        tempInput.setAttribute('readonly', 'readonly');
        tempInput.style.position = 'absolute';
        tempInput.style.left = '-9999px';
        document.body.appendChild(tempInput);
        tempInput.select();
        try{
            document.execCommand('copy');
            showToast('Token copied.');
        } catch(error){
            showToast('Unable to copy token.', true);
        } finally {
            document.body.removeChild(tempInput);
        }
    }

    function handleTokenManagementClick(event){
        var actionButton = event.target.closest('[data-token-action]');
        if(!actionButton){
            return;
        }

        var action = actionButton.getAttribute('data-token-action');
        if(action === 'refresh'){
            loadTokens(true);
            return;
        }
        if(action === 'copy-created'){
            copyCreatedToken();
            return;
        }
        if(action === 'revoke'){
            revokeTokenFromSettings(actionButton.getAttribute('data-token-id'));
        }
    }

    function detectBrowserTimezone(){
        try{
            var resolved = Intl.DateTimeFormat().resolvedOptions();
            if(resolved && resolved.timeZone){
                return resolved.timeZone;
            }
        } catch(error){}
        return 'UTC';
    }

    function renderTimezoneSelect(currentValue, isDisabled){
        var timezones = [
            'UTC',
            'America/New_York','America/Chicago','America/Denver','America/Los_Angeles',
            'America/Toronto','America/Vancouver','America/Sao_Paulo','America/Mexico_City',
            'Europe/London','Europe/Paris','Europe/Berlin','Europe/Amsterdam','Europe/Madrid',
            'Europe/Rome','Europe/Zurich','Europe/Stockholm','Europe/Warsaw','Europe/Moscow',
            'Asia/Tokyo','Asia/Shanghai','Asia/Hong_Kong','Asia/Singapore','Asia/Seoul',
            'Asia/Kolkata','Asia/Dubai','Asia/Bangkok',
            'Australia/Sydney','Australia/Melbourne','Pacific/Auckland',
            'Africa/Cairo','Africa/Johannesburg','Africa/Lagos'
        ];
        var options = "<option value=''>— Not set —</option>";
        for(var i=0;i<timezones.length;i++){
            var tz = timezones[i];
            var selected = tz === currentValue ? ' selected' : '';
            options += "<option value='" + escapeHtml(tz) + "'" + selected + ">" + escapeHtml(tz) + "</option>";
        }
        if(currentValue && timezones.indexOf(currentValue) === -1){
            options += "<option value='" + escapeHtml(currentValue) + "' selected>" + escapeHtml(currentValue) + "</option>";
        }
        return "<select id='profileTimezoneInput' class='control'" + (isDisabled ? ' disabled' : '') + ">" + options + "</select>";
    }

    function renderLanguageSelect(currentValue, isDisabled){
        var languages = [
            { code:'en-US', label:'English (US)' },
            { code:'en-GB', label:'English (UK)' },
            { code:'es-ES', label:'Spanish' },
            { code:'fr-FR', label:'French' },
            { code:'de-DE', label:'German' },
            { code:'it-IT', label:'Italian' },
            { code:'pt-BR', label:'Portuguese (Brazil)' },
            { code:'nl-NL', label:'Dutch' },
            { code:'sv-SE', label:'Swedish' },
            { code:'pl-PL', label:'Polish' },
            { code:'ru-RU', label:'Russian' },
            { code:'ja-JP', label:'Japanese' },
            { code:'zh-CN', label:'Chinese (Simplified)' },
            { code:'zh-TW', label:'Chinese (Traditional)' },
            { code:'ko-KR', label:'Korean' },
            { code:'ar-SA', label:'Arabic' },
            { code:'hi-IN', label:'Hindi' }
        ];
        var options = "<option value=''>— Not set —</option>";
        var matched = false;
        for(var i=0;i<languages.length;i++){
            var lang = languages[i];
            var selected = lang.code === currentValue ? ' selected' : '';
            if(selected){ matched = true; }
            options += "<option value='" + escapeHtml(lang.code) + "'" + selected + ">" + escapeHtml(lang.label) + "</option>";
        }
        if(currentValue && !matched){
            options += "<option value='" + escapeHtml(currentValue) + "' selected>" + escapeHtml(currentValue) + "</option>";
        }
        return "<select id='profileLanguageInput' class='control'" + (isDisabled ? ' disabled' : '') + ">" + options + "</select>";
    }

    function renderProfileSection(){
        var state = dashboardState.preferences;
        if(state.isLoading && !state.groups){
            return renderLoadingPanel('Loading profile…');
        }

        var nameEntry = findPreferenceEntry(state.groups, 'user.name');
        var emailEntry = findPreferenceEntry(state.groups, 'user.email');
        var timezoneEntry = findPreferenceEntry(state.groups, 'user.timezone');
        var langEntry = findPreferenceEntry(state.groups, 'user.preferredLanguage');
        var assistantNameEntry = findPreferenceEntry(state.groups, 'assistant.name');

        var detectedTz = detectBrowserTimezone();
        var currentTz = timezoneEntry ? timezoneEntry.value : detectedTz;
        var currentLang = langEntry ? langEntry.value : (navigator.language || 'en-US');
        var currentAssistantName = assistantNameEntry ? assistantNameEntry.value : '';

        return `
<form id='profileForm'>
    <div class='section-heading'>
        <div>
            <h2>Profile</h2>
            <p>Your identity and locale preferences used across OpenVEPA.</p>
        </div>
        <div class='settings-actions'>
            <button type='submit' class='primary-button' ${state.isMutating ? 'disabled' : ''}>${state.isMutating ? 'Saving…' : 'Save profile'}</button>
        </div>
    </div>
    <div class='field'>
        <span>Name</span>
        <input id='profileNameInput' class='control' type='text' value='${escapeHtml(nameEntry ? nameEntry.value : '')}' placeholder='Your name' ${state.isMutating ? 'disabled' : ''}>
    </div>
    <div class='field'>
        <span>Email</span>
        <input id='profileEmailInput' class='control' type='email' value='${escapeHtml(emailEntry ? emailEntry.value : '')}' placeholder='name@example.com' ${state.isMutating ? 'disabled' : ''}>
    </div>
    <div class='field'>
        <span>Timezone</span>
        ${renderTimezoneSelect(currentTz, state.isMutating)}
    </div>
    <div class='field'>
        <span>Preferred Language</span>
        ${renderLanguageSelect(currentLang, state.isMutating)}
    </div>
    <div class='field'>
        <span>Assistant Name</span>
        <input id='profileAssistantNameInput' class='control' type='text' value='${escapeHtml(currentAssistantName)}' placeholder='VEPA' maxlength='40' ${state.isMutating ? 'disabled' : ''}>
        <span class='helper-text'>Customize the display name for your assistant. Leave blank to use the default (VEPA).</span>
    </div>
    <p class='helper-text'>Leave a field blank to remove that stored value. Timezone auto-detects from your browser if not set.</p>
</form>`;
    }

    function renderAssistantPreferencesSection(){
        var state = dashboardState.preferences;
        if(state.isLoading && !state.groups){
            return renderLoadingPanel('Loading assistant preferences…');
        }

        var riskEntry = findPreferenceEntry(state.groups, 'assistant.riskTolerance');
        var commEntry = findPreferenceEntry(state.groups, 'assistant.communicationStyle');
        var autonomyEntry = findPreferenceEntry(state.groups, 'assistant.autonomyLevel');
        var currentRisk = riskEntry ? riskEntry.value : '';
        var currentComm = commEntry ? commEntry.value : '';
        var currentAutonomy = autonomyEntry ? autonomyEntry.value : '';

        return `
<form id='assistantPreferencesForm'>
    <div class='section-heading'>
        <div>
            <h2>Assistant Preferences</h2>
            <p>These help the assistant act autonomously with less clarification and better defaults.</p>
        </div>
        <div class='settings-actions'>
            <button type='submit' class='primary-button' ${state.isMutating ? 'disabled' : ''}>${state.isMutating ? 'Saving…' : 'Save preferences'}</button>
        </div>
    </div>
    <div class='field'>
        <span>Risk Tolerance</span>
        <select id='assistantRiskInput' class='control' ${state.isMutating ? 'disabled' : ''}>
            <option value=''>— Not set —</option>
            <option value='conservative'${currentRisk === 'conservative' ? ' selected' : ''}>Conservative</option>
            <option value='balanced'${currentRisk === 'balanced' ? ' selected' : ''}>Balanced</option>
            <option value='aggressive'${currentRisk === 'aggressive' ? ' selected' : ''}>Aggressive</option>
        </select>
    </div>
    <div class='field'>
        <span>Communication Style</span>
        <select id='assistantCommunicationInput' class='control' ${state.isMutating ? 'disabled' : ''}>
            <option value=''>— Not set —</option>
            <option value='professional'${currentComm === 'professional' ? ' selected' : ''}>Professional</option>
            <option value='friendly'${currentComm === 'friendly' ? ' selected' : ''}>Friendly</option>
            <option value='concise'${currentComm === 'concise' ? ' selected' : ''}>Concise</option>
            <option value='detailed'${currentComm === 'detailed' ? ' selected' : ''}>Detailed</option>
            <option value='technical'${currentComm === 'technical' ? ' selected' : ''}>Technical</option>
        </select>
    </div>
    <div class='field'>
        <span>Autonomy Level</span>
        <select id='assistantAutonomyInput' class='control' ${state.isMutating ? 'disabled' : ''}>
            <option value=''>— Not set —</option>
            <option value='ask-always'${currentAutonomy === 'ask-always' ? ' selected' : ''}>Ask always</option>
            <option value='ask-important'${currentAutonomy === 'ask-important' ? ' selected' : ''}>Ask for important decisions</option>
            <option value='act-autonomously'${currentAutonomy === 'act-autonomously' ? ' selected' : ''}>Act autonomously</option>
            <option value='full-autonomy'${currentAutonomy === 'full-autonomy' ? ' selected' : ''}>Full autonomy</option>
        </select>
    </div>
</form>`;
    }

    function renderAddPreferenceSection(){
        var state = dashboardState.preferences;
        if(state.isLoading && !state.groups){
            return renderLoadingPanel('Loading preference form…');
        }

        return `
<form id='addPreferenceForm'>
    <div class='section-heading'>
        <div>
            <h2>Add preference</h2>
            <p>Create or overwrite preferences with a key, value, and category.</p>
        </div>
        <div class='settings-actions'>
            <button type='submit' class='primary-button' ${state.isMutating ? 'disabled' : ''}>${state.isMutating ? 'Saving…' : 'Add preference'}</button>
        </div>
    </div>
    <div class='field'>
        <span>Key</span>
        <input id='addPreferenceKey' class='control' type='text' placeholder='assistant.riskTolerance' ${state.isMutating ? 'disabled' : ''}>
    </div>
    <div class='field'>
        <span>Value</span>
        <input id='addPreferenceValue' class='control' type='text' placeholder='balanced' ${state.isMutating ? 'disabled' : ''}>
    </div>
    <div class='field'>
        <span>Category</span>
        <input id='addPreferenceCategory' class='control' type='text' placeholder='assistant' ${state.isMutating ? 'disabled' : ''}>
    </div>
</form>`;
    }

    function renderPreferenceGroups(){
        var state = dashboardState.preferences;
        if(state.isLoading && !state.groups){
            return renderLoadingPanel('Loading preferences…');
        }
        if(!state.groups){
            return renderSettingsEmptyState('Preferences unavailable', 'The preference store could not be loaded.', 'Retry', "data-preferences-action='reload'");
        }

        var categoryNames = [];
        for(var category in state.groups){
            if(Object.prototype.hasOwnProperty.call(state.groups, category)){
                categoryNames.push(category);
            }
        }
        categoryNames.sort(function(left, right){
            return left.localeCompare(right);
        });

        if(categoryNames.length === 0){
            return renderSettingsEmptyState('No preferences stored yet.', 'Create your first preference using the form above.', '', '');
        }

        var html = '';
        for(var i=0;i<categoryNames.length;i++){
            var categoryName = categoryNames[i];
            var entries = state.groups[categoryName] || {};
            var keys = [];
            for(var key in entries){
                if(Object.prototype.hasOwnProperty.call(entries, key)){
                    keys.push(key);
                }
            }
            keys.sort(function(left, right){
                return left.localeCompare(right);
            });

            var rows = '';
            for(var j=0;j<keys.length;j++){
                var entry = entries[keys[j]];
                var isEditing = state.editingKey === entry.key;
                if(isEditing){
                    rows += `
<tr>
    <td>
        <div class='preference-key'>${escapeHtml(entry.key)}</div>
        <div class='preference-meta'>Updated ${escapeHtml(formatUpdatedAt(entry.updatedAt))}</div>
    </td>
    <td><input id='preferenceEditValue' class='control' type='text' value='${escapeHtml(entry.value)}' ${state.isMutating ? 'disabled' : ''}></td>
    <td><input id='preferenceEditCategory' class='control' type='text' value='${escapeHtml(entry.category)}' ${state.isMutating ? 'disabled' : ''}></td>
    <td>${escapeHtml(formatConfidence(entry.confidence))}</td>
    <td>${escapeHtml(getPreferenceSourceLabel(entry.source))}</td>
    <td>
        <div class='preference-actions'>
            <button type='button' class='primary-button' data-preference-action='save' data-preference-key='${escapeHtml(entry.key)}' ${state.isMutating ? 'disabled' : ''}>Save</button>
            <button type='button' class='secondary-button' data-preference-action='cancel' ${state.isMutating ? 'disabled' : ''}>Cancel</button>
        </div>
    </td>
</tr>`;
                } else {
                    rows += `
<tr>
    <td>
        <div class='preference-key'>${escapeHtml(entry.key)}</div>
        <div class='preference-meta'>Updated ${escapeHtml(formatUpdatedAt(entry.updatedAt))}</div>
    </td>
    <td>${escapeHtml(entry.value)}</td>
    <td>${escapeHtml(entry.category)}</td>
    <td>${escapeHtml(formatConfidence(entry.confidence))}</td>
    <td>${escapeHtml(getPreferenceSourceLabel(entry.source))}</td>
    <td>
        <div class='preference-actions'>
            <button type='button' class='secondary-button' data-preference-action='edit' data-preference-key='${escapeHtml(entry.key)}' ${state.isMutating ? 'disabled' : ''}>Edit</button>
            <button type='button' class='secondary-button' data-preference-action='delete' data-preference-key='${escapeHtml(entry.key)}' ${state.isMutating ? 'disabled' : ''}>Delete</button>
        </div>
    </td>
</tr>`;
                }
            }

            html += `
<section class='card preference-group'>
    <div class='section-heading'>
        <div>
            <h2>${escapeHtml(categoryName)}</h2>
            <p>${escapeHtml(formatNumber(keys.length))} ${escapeHtml(pluralize(keys.length, 'preference'))} in this category.</p>
        </div>
    </div>
    <div class='table-shell'>
        <table class='preference-table'>
            <thead>
                <tr>
                    <th>Key</th>
                    <th>Value</th>
                    <th>Category</th>
                    <th>Confidence</th>
                    <th>Source</th>
                    <th>Actions</th>
                </tr>
            </thead>
            <tbody>${rows}
            </tbody>
        </table>
    </div>
</section>`;
        }

        return html;
    }

    function renderSystemSection(){
        var state = dashboardState.restart;
        var statusHtml = '';
        if(state.isRestarting && !state.polling){
            statusHtml = '<div class="restart-status restart-status-restarting"><span class="loading-indicator" aria-hidden="true"><span></span><span></span><span></span></span> <span>' + escapeHtml(state.message || 'Restarting…') + '</span></div>';
        } else if(state.polling){
            statusHtml = '<div class="restart-status restart-status-polling"><span class="loading-indicator" aria-hidden="true"><span></span><span></span><span></span></span> <span>' + escapeHtml(state.message || 'Waiting for service…') + '</span></div>';
        } else if(state.message){
            statusHtml = '<div class="restart-status">' + escapeHtml(state.message) + '</div>';
        }

        return '<div class="section-heading"><div><h2>System</h2><p>Service management and diagnostics.</p></div></div>' +
            '<div class="restart-section">' +
            '<h3>Restart Service</h3>' +
            '<p class="helper-text">Restart the OpenVEPA service to apply configuration changes or recover from issues. Docker will automatically restart the container.</p>' +
            '<button type="button" class="secondary-button" id="restartServiceButton"' + (state.isRestarting ? ' disabled' : '') + '>' + (state.isRestarting ? 'Restarting…' : '🔄 Restart Service') + '</button>' +
            statusHtml +
            '</div>';
    }

    function renderUserSettingsSections(){
        var prefState = dashboardState.preferences;
        var tokenState = dashboardState.tokens;

        var statusHost = document.getElementById('userSettingsStatus');
        if(statusHost){
            var prefBanner = renderStatusBanner(prefState.error, 'error') || renderStatusBanner(prefState.message, 'success');
            var tokenBanner = renderStatusBanner(tokenState.error, 'error') || renderStatusBanner(tokenState.message, 'success');
            statusHost.innerHTML = prefBanner || tokenBanner;
        }

        var profileSection = document.getElementById('profileSection');
        if(profileSection){
            profileSection.innerHTML = renderProfileSection();
            var profileForm = document.getElementById('profileForm');
            if(profileForm){
                profileForm.onsubmit = function(event){
                    event.preventDefault();
                    saveProfileSettings();
                };
            }
        }

        var assistantSection = document.getElementById('assistantPreferencesSection');
        if(assistantSection){
            assistantSection.innerHTML = renderAssistantPreferencesSection();
            var assistantForm = document.getElementById('assistantPreferencesForm');
            if(assistantForm){
                assistantForm.onsubmit = function(event){
                    event.preventDefault();
                    saveAssistantPreferences();
                };
            }
        }

        var tokenManagementSection = document.getElementById('tokenManagementSection');
        if(tokenManagementSection){
            tokenManagementSection.innerHTML = renderTokenManagementSection();
            var tokenCreateForm = document.getElementById('tokenCreateForm');
            if(tokenCreateForm){
                tokenCreateForm.onsubmit = function(event){
                    event.preventDefault();
                    createTokenFromSettings();
                };
            }
            tokenManagementSection.onclick = handleTokenManagementClick;
        }

        var addPreferenceSection = document.getElementById('addPreferenceSection');
        if(addPreferenceSection){
            addPreferenceSection.innerHTML = renderAddPreferenceSection();
            var addPreferenceForm = document.getElementById('addPreferenceForm');
            if(addPreferenceForm){
                addPreferenceForm.onsubmit = function(event){
                    event.preventDefault();
                    saveNewPreference();
                };
            }
        }

        var preferenceGroups = document.getElementById('preferenceGroups');
        if(preferenceGroups){
            preferenceGroups.innerHTML = renderPreferenceGroups();
            preferenceGroups.onclick = handlePreferenceGroupsClick;
            var reloadButton = preferenceGroups.querySelector('[data-preferences-action="reload"]');
            if(reloadButton){
                reloadButton.onclick = function(){ loadPreferences(true); };
            }
        }

        var systemSection = document.getElementById('systemSection');
        if(systemSection){
            systemSection.innerHTML = renderSystemSection();
            var restartButton = document.getElementById('restartServiceButton');
            if(restartButton){
                restartButton.onclick = function(){ restartService(); };
            }
        }
    }

    function saveProfileSettings(){
        var state = dashboardState.preferences;
        var nameInput = document.getElementById('profileNameInput');
        var emailInput = document.getElementById('profileEmailInput');
        var timezoneInput = document.getElementById('profileTimezoneInput');
        var languageInput = document.getElementById('profileLanguageInput');
        var assistantNameInput = document.getElementById('profileAssistantNameInput');
        if(!nameInput || !emailInput){
            return;
        }

        var operations = [];
        var nameValue = nameInput.value.trim();
        var emailValue = emailInput.value.trim();
        var timezoneValue = timezoneInput ? timezoneInput.value : '';
        var languageValue = languageInput ? languageInput.value : '';
        var assistantNameValue = assistantNameInput ? assistantNameInput.value.trim() : '';
        var existingName = findPreferenceEntry(state.groups, 'user.name');
        var existingEmail = findPreferenceEntry(state.groups, 'user.email');
        var existingTimezone = findPreferenceEntry(state.groups, 'user.timezone');
        var existingLanguage = findPreferenceEntry(state.groups, 'user.preferredLanguage');
        var existingAssistantName = findPreferenceEntry(state.groups, 'assistant.name');

        if(nameValue){
            operations.push(upsertPreference('user.name', nameValue, 'personal'));
        } else if(existingName){
            operations.push(deletePreference('user.name'));
        }

        if(emailValue){
            operations.push(upsertPreference('user.email', emailValue, 'personal'));
        } else if(existingEmail){
            operations.push(deletePreference('user.email'));
        }

        if(timezoneValue){
            operations.push(upsertPreference('user.timezone', timezoneValue, 'locale'));
        } else if(existingTimezone){
            operations.push(deletePreference('user.timezone'));
        }

        if(languageValue){
            operations.push(upsertPreference('user.preferredLanguage', languageValue, 'locale'));
        } else if(existingLanguage){
            operations.push(deletePreference('user.preferredLanguage'));
        }

        if(assistantNameValue){
            operations.push(upsertPreference('assistant.name', assistantNameValue, 'assistant'));
        } else if(existingAssistantName){
            operations.push(deletePreference('assistant.name'));
        }

        if(operations.length === 0){
            state.error = '';
            state.message = 'Profile is already up to date.';
            renderUserSettingsSections();
            return;
        }

        runPreferenceMutation(function(){
            return Promise.all(operations).then(function(){
                var newName = assistantNameValue || 'VEPA';
                dashboardState.preferences.assistantName = newName;
                applyAssistantName(newName);
            });
        }, 'Profile saved.');
    }

    function saveNewPreference(){
        var keyInput = document.getElementById('addPreferenceKey');
        var valueInput = document.getElementById('addPreferenceValue');
        var categoryInput = document.getElementById('addPreferenceCategory');
        if(!keyInput || !valueInput || !categoryInput){
            return;
        }

        var key = keyInput.value.trim();
        var value = valueInput.value.trim();
        var category = categoryInput.value.trim();
        if(!key || !value || !category){
            dashboardState.preferences.error = 'Key, value, and category are required.';
            renderUserSettingsSections();
            return;
        }

        runPreferenceMutation(function(){
            return upsertPreference(key, value, category);
        }, 'Preference saved.');
    }

    function saveAssistantPreferences(){
        var state = dashboardState.preferences;
        var riskInput = document.getElementById('assistantRiskInput');
        var commInput = document.getElementById('assistantCommunicationInput');
        var autonomyInput = document.getElementById('assistantAutonomyInput');

        var operations = [];
        var riskValue = riskInput ? riskInput.value : '';
        var commValue = commInput ? commInput.value : '';
        var autonomyValue = autonomyInput ? autonomyInput.value : '';
        var existingRisk = findPreferenceEntry(state.groups, 'assistant.riskTolerance');
        var existingComm = findPreferenceEntry(state.groups, 'assistant.communicationStyle');
        var existingAutonomy = findPreferenceEntry(state.groups, 'assistant.autonomyLevel');

        if(riskValue){
            operations.push(upsertPreference('assistant.riskTolerance', riskValue, 'assistant'));
        } else if(existingRisk){
            operations.push(deletePreference('assistant.riskTolerance'));
        }

        if(commValue){
            operations.push(upsertPreference('assistant.communicationStyle', commValue, 'assistant'));
        } else if(existingComm){
            operations.push(deletePreference('assistant.communicationStyle'));
        }

        if(autonomyValue){
            operations.push(upsertPreference('assistant.autonomyLevel', autonomyValue, 'assistant'));
        } else if(existingAutonomy){
            operations.push(deletePreference('assistant.autonomyLevel'));
        }

        if(operations.length === 0){
            state.error = '';
            state.message = 'Assistant preferences are already up to date.';
            renderUserSettingsSections();
            return;
        }

        runPreferenceMutation(function(){
            return Promise.all(operations);
        }, 'Assistant preferences saved.');
    }

    function handlePreferenceGroupsClick(event){
        var actionButton = event.target.closest('[data-preference-action]');
        if(!actionButton){
            return;
        }

        var action = actionButton.getAttribute('data-preference-action');
        var key = actionButton.getAttribute('data-preference-key');
        if(action === 'edit'){
            dashboardState.preferences.editingKey = key;
            dashboardState.preferences.error = '';
            renderUserSettingsSections();
            return;
        }
        if(action === 'cancel'){
            dashboardState.preferences.editingKey = '';
            dashboardState.preferences.error = '';
            renderUserSettingsSections();
            return;
        }
        if(action === 'save'){
            var valueInput = document.getElementById('preferenceEditValue');
            var categoryInput = document.getElementById('preferenceEditCategory');
            if(!valueInput || !categoryInput){
                return;
            }

            var value = valueInput.value.trim();
            var category = categoryInput.value.trim();
            if(!value || !category){
                dashboardState.preferences.error = 'Value and category are required.';
                renderUserSettingsSections();
                return;
            }

            runPreferenceMutation(function(){
                return upsertPreference(key, value, category);
            }, 'Preference updated.');
            return;
        }
        if(action === 'delete' && key){
            if(window.confirm('Delete preference ' + key + '?')){
                runPreferenceMutation(function(){
                    return deletePreference(key);
                }, 'Preference deleted.');
            }
        }
    }

    function loadAssistantName(){
        apiRequest('/api/preferences').then(function(payload){
            var groups = normalizePreferencesPayload(payload);
            var entry = findPreferenceEntry(groups, 'assistant.name');
            var name = entry ? entry.value : 'VEPA';
            dashboardState.preferences.assistantName = name;
            applyAssistantName(name);
        }).catch(function(){
            applyAssistantName('VEPA');
        });
    }

    function applyAssistantName(name){
        var displayName = name || 'VEPA';
        var topbar = document.getElementById('topbarTitle');
        var sidebar = document.getElementById('sidebarBrandTitle');
        if(topbar){ topbar.textContent = displayName; }
        if(sidebar){ sidebar.textContent = displayName; }
        document.title = displayName + ' WebUI';
    }

    function restartService(){
        if(!window.confirm('Are you sure? The service will restart and you will be disconnected briefly.')){
            return;
        }

        var state = dashboardState.restart;
        state.isRestarting = true;
        state.polling = false;
        state.message = 'Sending restart request…';
        renderUserSettingsSections();

        apiRequest('/api/system/restart', { method:'POST' }).then(function(result){
            state.message = (result && result.message) || 'Service is restarting…';
            state.isRestarting = true;
            renderUserSettingsSections();
            window.setTimeout(function(){ pollHealthAfterRestart(); }, 4000);
        }).catch(function(err){
            state.isRestarting = false;
            state.message = '';
            dashboardState.preferences.error = 'Restart failed: ' + (err.message || 'Unknown error');
            renderUserSettingsSections();
        });
    }

    function pollHealthAfterRestart(){
        var state = dashboardState.restart;
        state.polling = true;
        state.message = 'Waiting for service to come back online…';
        renderUserSettingsSections();

        var attempts = 0;
        var maxAttempts = 30;
        var pollInterval = 2000;

        function poll(){
            attempts++;
            fetch('/health').then(function(response){
                if(response.ok){
                    state.polling = false;
                    state.isRestarting = false;
                    state.message = '';
                    window.location.reload();
                } else if(attempts < maxAttempts){
                    window.setTimeout(poll, pollInterval);
                } else {
                    state.polling = false;
                    state.isRestarting = false;
                    state.message = 'Service did not come back online within 60 seconds. Try refreshing manually.';
                    renderUserSettingsSections();
                }
            }).catch(function(){
                if(attempts < maxAttempts){
                    window.setTimeout(poll, pollInterval);
                } else {
                    state.polling = false;
                    state.isRestarting = false;
                    state.message = 'Service did not come back online within 60 seconds. Try refreshing manually.';
                    renderUserSettingsSections();
                }
            });
        }

        poll();
    }

    sidebarToggle.addEventListener('click', toggleSidebar);
    sidebarOverlay.addEventListener('click', closeMobileSidebar);

    document.getElementById('themePicker').addEventListener('click', function(event){
        var btn = event.target.closest('.theme-btn');
        if(btn){
            var pref = btn.getAttribute('data-theme-pref');
            if(pref) applyTheme(pref);
        }
    });

    navList.addEventListener('click', function(event){
        var target = event.target;
        if(target && target.closest && target.closest('.nav-link') && isMobile()){
            closeMobileSidebar();
        }
    });

    window.addEventListener('hashchange', renderRoute);
    window.addEventListener('keydown', function(event){
        if(event.key === 'Escape'){
            closeMobileSidebar();
        }
    });

    if(mobileMedia.addEventListener){
        mobileMedia.addEventListener('change', syncSidebarState);
    } else if(mobileMedia.addListener){
        mobileMedia.addListener(syncSidebarState);
    }

    /* Sessions page */
    var sessionsPageState = {
        items: [],
        filterText: '',
        isCreateFormOpen: false,
        isLoading: false,
        isCreating: false,
        errorMessage: '',
        requestId: 0
    };
    var activeSessionStorageKey = 'openvepa_active_session_id';

    function renderSessionsPage(){
        if(window.requestAnimationFrame){
            window.requestAnimationFrame(initializeSessionsPage);
        } else {
            window.setTimeout(initializeSessionsPage, 0);
        }

        return `
<!-- Page: Sessions -->
<section class="page-shell">
    <header class="page-header">
        <div>
            <h1>Sessions</h1>
            <p>Browse saved conversations, search by title, and start a new assistant session without leaving the dashboard.</p>
        </div>
        <button type="button" class="primary-button sessions-create-toggle" data-session-create-toggle="true">+ New Session</button>
    </header>

    <section class="sessions-layout">
        <section class="card sessions-toolbar-card">
            <div class="sessions-toolbar-row">
                <label class="sessions-search" for="sessionsSearchInput">
                    <span class="sessions-search-icon" aria-hidden="true">🔍</span>
                    <input id="sessionsSearchInput" class="control sessions-search-input" type="search" placeholder="Search sessions..." autocomplete="off">
                </label>
            </div>

            <form class="sessions-create-form" id="sessionsCreateForm" hidden>
                <label class="field sessions-create-field" for="sessionsTitleInput">
                    <span>Session title</span>
                    <input id="sessionsTitleInput" class="control" type="text" maxlength="120" placeholder="Optional title">
                </label>
                <div class="sessions-create-actions">
                    <button type="submit" class="primary-button" id="sessionsCreateSubmit">Create</button>
                    <button type="button" class="secondary-button" id="sessionsCreateCancel">Cancel</button>
                </div>
            </form>

            <p class="helper-text sessions-feedback" id="sessionsFeedback">Loading sessions...</p>
        </section>

        <section class="sessions-list" id="sessionsList" aria-live="polite"></section>
    </section>
</section>`;
    }

    function initializeSessionsPage(){
        if(getRoute().hash !== '#/sessions'){
            return;
        }

        var searchInput = document.getElementById('sessionsSearchInput');
        var createForm = document.getElementById('sessionsCreateForm');
        var createCancelButton = document.getElementById('sessionsCreateCancel');
        var sessionsList = document.getElementById('sessionsList');
        var createToggleButtons = document.querySelectorAll('[data-session-create-toggle="true"]');

        if(!searchInput || !createForm || !createCancelButton || !sessionsList){
            return;
        }

        searchInput.value = sessionsPageState.filterText;
        searchInput.addEventListener('input', onSessionsSearchInput);
        createForm.addEventListener('submit', onSessionsCreateSubmit);
        createCancelButton.addEventListener('click', function(){
            setSessionsCreateFormOpen(false);
        });

        for(var i=0;i<createToggleButtons.length;i++){
            createToggleButtons[i].addEventListener('click', function(){
                setSessionsCreateFormOpen(true);
            });
        }

        sessionsList.addEventListener('click', onSessionsListClick);

        setSessionsCreateFormOpen(sessionsPageState.isCreateFormOpen);
        renderSessionsList();
        updateSessionsFeedback();
        loadSessions();
    }

    function onSessionsSearchInput(event){
        sessionsPageState.filterText = event.target.value || '';
        renderSessionsList();
        updateSessionsFeedback();
    }

    async function onSessionsCreateSubmit(event){
        event.preventDefault();

        if(sessionsPageState.isCreating){
            return;
        }

        var titleInput = document.getElementById('sessionsTitleInput');
        if(!titleInput){
            return;
        }

        sessionsPageState.isCreating = true;
        syncSessionsCreateForm();

        try{
            var headers = getAuthHeaders();
            headers['Content-Type'] = 'application/json';

            var response = await fetch('/api/sessions', {
                method:'POST',
                headers:headers,
                body:JSON.stringify({
                    title:(titleInput.value || '').trim() || null
                })
            });

            var payload = await readJsonResponse(response);
            if(!response.ok){
                throw new Error(getApiErrorMessage(response, payload, 'Unable to create a session.'));
            }

            upsertSession(payload);
            sessionsPageState.filterText = '';
            titleInput.value = '';
            setSessionsCreateFormOpen(false);
            renderSessionsList();
            updateSessionsFeedback();
            showToast('Session created.');
        } catch (error){
            showToast(error && error.message ? error.message : 'Unable to create a session.', true);
        } finally {
            sessionsPageState.isCreating = false;
            syncSessionsCreateForm();
        }
    }

    function onSessionsListClick(event){
        var actionButton = event.target && event.target.closest ? event.target.closest('[data-session-action]') : null;
        if(!actionButton){
            return;
        }

        var action = actionButton.getAttribute('data-session-action');
        var sessionId = actionButton.getAttribute('data-session-id');

        if(action === 'open' && sessionId){
            localStorage.setItem(activeSessionStorageKey, sessionId);
            window.location.hash = '#/home';
            return;
        }

        if(action === 'delete'){
            showToast('Not available yet');
            return;
        }

        if(action === 'clear-search'){
            sessionsPageState.filterText = '';
            var searchInput = document.getElementById('sessionsSearchInput');
            if(searchInput){
                searchInput.value = '';
                searchInput.focus();
            }
            renderSessionsList();
            updateSessionsFeedback();
            return;
        }

        if(action === 'create'){
            setSessionsCreateFormOpen(true);
        }
    }

    async function loadSessions(){
        var requestId = ++sessionsPageState.requestId;
        sessionsPageState.isLoading = true;
        sessionsPageState.errorMessage = '';
        renderSessionsList();
        updateSessionsFeedback();

        try{
            var response = await fetch('/api/sessions', {
                headers:getAuthHeaders()
            });
            var payload = await readJsonResponse(response);
            if(!response.ok){
                throw new Error(getApiErrorMessage(response, payload, 'Unable to load sessions.'));
            }

            if(requestId !== sessionsPageState.requestId){
                return;
            }

            sessionsPageState.items = sortSessions(Array.isArray(payload) ? payload : []);
            sessionsPageState.errorMessage = '';
        } catch (error){
            if(requestId !== sessionsPageState.requestId){
                return;
            }

            sessionsPageState.items = [];
            sessionsPageState.errorMessage = error && error.message ? error.message : 'Unable to load sessions.';
        } finally {
            if(requestId !== sessionsPageState.requestId){
                return;
            }

            sessionsPageState.isLoading = false;
            renderSessionsList();
            updateSessionsFeedback();
        }
    }

    function renderSessionsList(){
        if(getRoute().hash !== '#/sessions'){
            return;
        }

        var sessionsList = document.getElementById('sessionsList');
        if(!sessionsList){
            return;
        }

        var filteredSessions = getFilteredSessions();

        if(sessionsPageState.isLoading && !sessionsPageState.items.length){
            sessionsList.innerHTML = `
<section class="card sessions-empty-state">
    <div class="loading-indicator" aria-hidden="true"><span></span><span></span><span></span></div>
    <h2>Loading sessions...</h2>
    <p>Fetching your saved conversations from the OpenVEPA server.</p>
</section>`;
            return;
        }

        if(sessionsPageState.errorMessage){
            sessionsList.innerHTML = `
<section class="card sessions-empty-state sessions-empty-state-error">
    <div class="sessions-empty-icon" aria-hidden="true">⚠️</div>
    <h2>Unable to load sessions</h2>
    <p>${renderMessageWithLinks(sessionsPageState.errorMessage)}</p>
    <div class="sessions-empty-actions">
        <button type="button" class="secondary-button" data-session-action="create">New Session</button>
    </div>
</section>`;
            return;
        }

        if(!sessionsPageState.items.length){
            sessionsList.innerHTML = `
<section class="card sessions-empty-state">
    <div class="sessions-empty-icon" aria-hidden="true">💬</div>
    <h2>No sessions yet</h2>
    <p>No sessions yet. Start a new conversation!</p>
    <div class="sessions-empty-actions">
        <button type="button" class="primary-button" data-session-action="create">+ New Session</button>
    </div>
</section>`;
            return;
        }

        if(!filteredSessions.length){
            sessionsList.innerHTML = `
<section class="card sessions-empty-state">
    <div class="sessions-empty-icon" aria-hidden="true">🔎</div>
    <h2>No matching sessions</h2>
    <p>Try a different search term or clear the current filter to see every saved conversation.</p>
    <div class="sessions-empty-actions">
        <button type="button" class="secondary-button" data-session-action="clear-search">Clear search</button>
    </div>
</section>`;
            return;
        }

        var html = '';
        for(var i=0;i<filteredSessions.length;i++){
            var session = filteredSessions[i];
            var statusInfo = getSessionStatusInfo(session.status);

            html += `
<article class="card session-card">
    <div class="session-card-main">
        <div class="session-card-header">
            <div class="session-card-title">
                <span class="session-card-title-icon" aria-hidden="true">📝</span>
                <span>${escapeHtml(getSessionTitle(session))}</span>
            </div>
            <span class="session-status-badge ${statusInfo.className}">${statusInfo.label}</span>
        </div>
        <p class="session-card-meta">Created: ${escapeHtml(formatSessionDate(session.createdAt))} · Updated: <span title="${escapeHtml(formatSessionDateTime(session.updatedAt))}">${escapeHtml(formatSessionDate(session.updatedAt))}</span></p>
        <div class="session-card-status">
            <span class="session-card-secondary">Session ID: ${escapeHtml(session.id || '')}</span>
        </div>
    </div>
    <div class="session-card-actions">
        <button type="button" class="secondary-button session-action-button" data-session-action="open" data-session-id="${escapeHtmlAttribute(session.id || '')}">Open</button>
        <button type="button" class="secondary-button session-action-button" data-session-action="delete" data-session-id="${escapeHtmlAttribute(session.id || '')}">Delete</button>
    </div>
</article>`;
        }

        sessionsList.innerHTML = html;
    }

    function updateSessionsFeedback(){
        if(getRoute().hash !== '#/sessions'){
            return;
        }

        var feedback = document.getElementById('sessionsFeedback');
        if(!feedback){
            return;
        }

        if(sessionsPageState.isLoading){
            feedback.textContent = sessionsPageState.items.length ? 'Refreshing sessions...' : 'Loading sessions...';
            return;
        }

        if(sessionsPageState.errorMessage){
            feedback.textContent = isTokenRecoveryMessage(sessionsPageState.errorMessage)
                ? 'Token missing or expired. Use Home or User Settings to recover access.'
                : sessionsPageState.errorMessage;
            return;
        }

        if(!sessionsPageState.items.length){
            feedback.textContent = 'No sessions yet. Start a new conversation!';
            return;
        }

        var filteredSessions = getFilteredSessions();
        if(sessionsPageState.filterText.trim()){
            feedback.textContent = 'Showing ' + filteredSessions.length + ' of ' + sessionsPageState.items.length + ' sessions.';
            return;
        }

        feedback.textContent = sessionsPageState.items.length + (sessionsPageState.items.length === 1 ? ' session loaded.' : ' sessions loaded.');
    }

    function setSessionsCreateFormOpen(isOpen){
        sessionsPageState.isCreateFormOpen = !!isOpen;
        syncSessionsCreateForm();
    }

    function syncSessionsCreateForm(){
        var createForm = document.getElementById('sessionsCreateForm');
        var titleInput = document.getElementById('sessionsTitleInput');
        var createSubmit = document.getElementById('sessionsCreateSubmit');
        var createToggleButtons = document.querySelectorAll('[data-session-create-toggle="true"]');

        if(createForm){
            createForm.hidden = !sessionsPageState.isCreateFormOpen;
        }

        if(createSubmit){
            createSubmit.disabled = sessionsPageState.isCreating;
            createSubmit.textContent = sessionsPageState.isCreating ? 'Creating...' : 'Create';
        }

        for(var i=0;i<createToggleButtons.length;i++){
            createToggleButtons[i].disabled = sessionsPageState.isCreating;
        }

        if(sessionsPageState.isCreateFormOpen && titleInput && !sessionsPageState.isCreating){
            titleInput.focus();
        }
    }

    function getFilteredSessions(){
        var filterText = (sessionsPageState.filterText || '').trim().toLowerCase();
        var items = sortSessions(sessionsPageState.items);

        if(!filterText){
            return items;
        }

        var filteredItems = [];
        for(var i=0;i<items.length;i++){
            if(getSessionTitle(items[i]).toLowerCase().indexOf(filterText) !== -1){
                filteredItems.push(items[i]);
            }
        }

        return filteredItems;
    }

    function sortSessions(items){
        return (items || []).slice().sort(function(left, right){
            return toSessionTime(right && right.updatedAt) - toSessionTime(left && left.updatedAt);
        });
    }

    function upsertSession(session){
        if(!session || !session.id){
            return;
        }

        var updatedItems = [];
        var wasUpdated = false;

        for(var i=0;i<sessionsPageState.items.length;i++){
            if(sessionsPageState.items[i].id === session.id){
                updatedItems.push(session);
                wasUpdated = true;
            } else {
                updatedItems.push(sessionsPageState.items[i]);
            }
        }

        if(!wasUpdated){
            updatedItems.push(session);
        }

        sessionsPageState.items = sortSessions(updatedItems);
    }

    function getSessionTitle(session){
        return session && session.title ? session.title : 'Untitled session';
    }

    function getSessionStatusInfo(status){
        var normalizedStatus = (status === null || typeof status === 'undefined') ? '' : String(status).toLowerCase();
        switch(normalizedStatus){
            case '0':
            case 'active':
                return { label:'Active', className:'session-status-active' };
            case '1':
            case 'paused':
                return { label:'Paused', className:'session-status-paused' };
            case '2':
            case 'completed':
                return { label:'Completed', className:'session-status-completed' };
            case '3':
            case 'archived':
            default:
                return { label:'Archived', className:'session-status-archived' };
        }
    }

    function formatSessionDate(value){
        var timestamp = toSessionTime(value);
        if(!timestamp){
            return 'Unknown date';
        }

        var diffMs = timestamp - Date.now();
        var absMs = Math.abs(diffMs);
        var minuteMs = 60 * 1000;
        var hourMs = 60 * minuteMs;
        var dayMs = 24 * hourMs;

        if(absMs < minuteMs){
            return 'Just now';
        }

        if(absMs < hourMs){
            return formatRelativeTime(diffMs, minuteMs, 'minute');
        }

        if(absMs < dayMs){
            return formatRelativeTime(diffMs, hourMs, 'hour');
        }

        if(absMs < 7 * dayMs){
            return formatRelativeTime(diffMs, dayMs, 'day');
        }

        return new Date(timestamp).toLocaleDateString(undefined, {
            month:'short',
            day:'numeric',
            year:'numeric'
        });
    }

    function formatRelativeTime(diffMs, unitMs, unit){
        var value = Math.round(diffMs / unitMs);
        if(!value){
            value = diffMs < 0 ? -1 : 1;
        }

        if(typeof Intl !== 'undefined' && typeof Intl.RelativeTimeFormat === 'function'){
            return new Intl.RelativeTimeFormat(undefined, { numeric:'auto' }).format(value, unit);
        }

        var absValue = Math.abs(value);
        var suffix = value < 0 ? ' ago' : ' from now';
        return absValue + ' ' + unit + (absValue === 1 ? '' : 's') + suffix;
    }

    function formatSessionDateTime(value){
        var timestamp = toSessionTime(value);
        return timestamp ? new Date(timestamp).toLocaleString() : 'Unknown date';
    }

    function toSessionTime(value){
        var timestamp = new Date(value || '').getTime();
        return isNaN(timestamp) ? 0 : timestamp;
    }

    function escapeHtml(value){
        return String(value === null || typeof value === 'undefined' ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function escapeHtmlAttribute(value){
        return escapeHtml(value).replace(/`/g, '&#96;');
    }

    async function readJsonResponse(response){
        try{
            return await response.json();
        } catch (error){
            return null;
        }
    }

    function getApiErrorMessage(response, payload, fallbackMessage){
        if(payload && payload.error){
            return payload.error;
        }

        if(response && (response.status === 401 || response.status === 403)){
            storeToken('');
            storeTokenId('');
            return getTokenRecoveryMessage();
        }

        if(response && response.status === 501){
            return 'Not available yet';
        }

        return fallbackMessage + (response ? ' (HTTP ' + response.status + ')' : '');
    }

    function showToast(message, isError){
        var toastContainer = getToastContainer();
        var toast = document.createElement('div');
        toast.className = 'sessions-toast' + (isError ? ' is-error' : '');
        toast.textContent = message;
        toastContainer.appendChild(toast);

        window.setTimeout(function(){
            toast.classList.add('is-leaving');
            window.setTimeout(function(){
                if(toast.parentNode){
                    toast.parentNode.removeChild(toast);
                }
            }, 220);
        }, 2600);
    }

    function getToastContainer(){
        var toastContainer = document.getElementById('sessionsToastContainer');
        if(toastContainer){
            return toastContainer;
        }

        toastContainer = document.createElement('div');
        toastContainer.id = 'sessionsToastContainer';
        toastContainer.className = 'sessions-toast-container';
        document.body.appendChild(toastContainer);
        return toastContainer;
    }

    function getAuthHeaders(){
        var token = getAuthToken();
        return token ? { 'Authorization': 'Bearer ' + token } : {};
    }

    renderNavigation();
    syncSidebarState();
    loadAssistantName();

    if(!routeMap[window.location.hash] && window.location.hash.indexOf('#/user-settings') !== 0){
        window.location.hash = '#/home';
    } else {
        renderRoute();
    }
})();
</script>
</body>
</html>
""";
}
