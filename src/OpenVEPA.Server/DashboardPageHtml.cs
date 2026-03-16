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
    min-height:100vh;
    overflow:hidden;
    position:relative;
    z-index:30;
    transition:transform .25s ease,width .25s ease;
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
    min-height:100vh;
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
    min-height:calc(100vh - 120px);
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
    .page-shell{min-height:calc(100vh - 104px)}
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
                    <div class="sidebar-brand-title">OpenVEPA</div>
                    <div class="sidebar-brand-subtitle">WebUI Navigation</div>
                </div>
            </div>
        </div>
        <div class="sidebar-caption">Workspace</div>
        <nav aria-label="WebUI sections">
            <ul class="nav-list" id="navList"></ul>
        </nav>
        <div class="sidebar-footer">
            Dark shell scaffold ready for feature-specific pages and SignalR chat wiring.
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
                        <span class="topbar-title">OpenVEPA</span>
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
        return routeMap[window.location.hash] || routeMap['#/home'];
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
            homeState.sessions = [];
            homeState.messages = [];
            homeState.sessionsLoaded = false;
            homeState.messagesLoadedFor = '';
            storeToken(nextToken);
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
        var busy = homeState.statusLoading || homeState.isLoadingSessions || homeState.isLoadingMessages || homeState.isSending || homeState.isCreatingSession;
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
<div class="error-banner">${escapeHtml(homeState.statusError)}</div>`;
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
        <p>Paste a token for this browser session. It will be stored in localStorage and sent on every REST request.</p>
    </div>
</div>
${homeState.authError ? `<div class="error-banner">${escapeHtml(homeState.authError)}</div>` : ''}
<form id="tokenForm" class="auth-form">
    <label class="field" for="tokenInput">
        <span>Bearer token</span>
        <input id="tokenInput" class="control" type="password" placeholder="Paste Bearer token" autocomplete="off" spellcheck="false" value="${escapeHtml(homeState.tokenDraft)}">
    </label>
    <div class="toolbar-actions">
        <button type="submit" class="primary-button">Save token</button>
    </div>
    <p class="helper-note">Once saved, the dashboard calls <code>/api/sessions</code> and <code>/api/sessions/{id}/messages</code> with an <code>Authorization: Bearer &lt;token&gt;</code> header.</p>
</form>`;
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
<div class="error-banner">${escapeHtml(homeState.statusError)}</div>
${renderEmptyState('Server unavailable', 'Fix the server status issue and refresh the page to continue.')}`;
            return;
        }

        if(homeState.status && homeState.status.setupComplete === false){
            host.innerHTML = renderEmptyState('Setup is not complete', 'Finish the setup wizard, then return to Home to start using chat sessions.');
            return;
        }

        if(!homeState.token){
            host.innerHTML = renderEmptyState('Authentication required', 'Enter a bearer token below to load or create assistant sessions in this browser.');
            return;
        }

        if(homeState.isLoadingSessions && homeState.sessions.length === 0){
            host.innerHTML = renderEmptyState('Loading sessions', 'Fetching your saved sessions from the REST API.');
            return;
        }

        if(homeState.sessionsError && homeState.sessions.length === 0){
            host.innerHTML = `
<div class="error-banner">${escapeHtml(homeState.sessionsError)}</div>
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
            }, 'Enter a valid bearer token to load chat sessions.');
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
            }, 'Enter a valid bearer token to load message history.');
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
            }, 'Enter a valid bearer token to create a new chat session.');
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
            }, 'Enter a valid bearer token to send a chat message.');
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
            return 'Provide a bearer token to load sessions and message history.';
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
            handleUnauthorized(unauthorizedMessage || 'Authentication failed.');
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
            return payload.detail || payload.title || payload.message || text;
        } catch (error) {
            return text;
        }
    }

    function handleUnauthorized(message){
        homeState.token = '';
        homeState.tokenDraft = '';
        homeState.authError = message;
        homeState.sessions = [];
        homeState.messages = [];
        homeState.sessionsLoaded = false;
        homeState.messagesLoadedFor = '';
        homeState.sessionsError = '';
        storeToken('');
        updateHomePage();
    }

    function clearToken(){
        homeState.token = '';
        homeState.tokenDraft = '';
        homeState.authError = '';
        homeState.sessions = [];
        homeState.messages = [];
        homeState.sessionsLoaded = false;
        homeState.messagesLoadedFor = '';
        homeState.sessionsError = '';
        storeToken('');
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
            draft:null,
            isEditing:false,
            isLoading:false,
            isSaving:false,
            message:'',
            error:''
        },
        agents:{
            items:null,
            detailByName:{},
            detailLoading:{},
            detailError:{},
            expandedName:'',
            isLoading:false,
            error:''
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
            error:''
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
        return window.localStorage.getItem('openvepa_token') || '';
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
                    if(response.status === 401){
                        message = token
                            ? 'Your OpenVEPA session has expired. Sign in again to continue.'
                            : 'OpenVEPA token missing. Sign in again to continue.';
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

    function renderStatusBanner(message, kind){
        if(!message){
            return '';
        }

        return `<div class='status-banner is-${kind || 'info'}'>${escapeHtml(message)}</div>`;
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
            <p>Review the current provider configuration, update provider defaults, and monitor aggregate model usage from the WebUI.</p>
        </div>
        <span class='pill'>Provider configuration</span>
    </header>
    <div class='settings-grid'>
        <section class='card settings-card' id='llmConfigCard'>${renderLoadingPanel('Loading LLM configuration…')}</section>
        <section class='card settings-card' id='llmUsageCard'>${renderLoadingPanel('Loading usage statistics…')}</section>
    </div>
</section>`;
    }

    function initLlmSettingsPage(){
        renderLlmSettingsPanels();
        if(!dashboardState.llm.config || !dashboardState.llm.usage){
            loadLlmSettings(false);
        }
    }

    function buildLlmDraft(){
        var config = dashboardState.llm.config || {};
        return {
            provider:config.provider || llmProviders[0],
            modelId:config.modelId || '',
            endpoint:config.endpoint || ''
        };
    }

    function loadLlmSettings(force){
        var state = dashboardState.llm;
        if(state.isLoading){
            return Promise.resolve();
        }
        if(!force && state.config && state.usage){
            renderLlmSettingsPanels();
            return Promise.resolve();
        }

        state.isLoading = true;
        state.error = '';
        renderLlmSettingsPanels();

        return Promise.all([
            apiRequest('/api/system/llm-config'),
            apiRequest('/api/system/llm-usage')
        ]).then(function(results){
            state.config = results[0];
            state.usage = results[1];
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isLoading = false;
            renderLlmSettingsPanels();
        });
    }

    function renderLlmProviderOptions(selectedProvider){
        var html = '';
        for(var i=0;i<llmProviders.length;i++){
            var provider = llmProviders[i];
            html += `<option value='${escapeHtml(provider)}'${provider === selectedProvider ? ' selected' : ''}>${escapeHtml(provider)}</option>`;
        }
        return html;
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
        if(state.isEditing){
            var draft = state.draft || buildLlmDraft();
            return `
<div class='section-heading'>
    <div>
        <h2>Edit configuration</h2>
        <p>Changes are written to appsettings.json. API keys remain hidden from the WebUI.</p>
    </div>
</div>
${banner}
<div class='field'>
    <span>Provider</span>
    <select id='llmProviderSelect' class='control' ${state.isSaving ? 'disabled' : ''}>${renderLlmProviderOptions(draft.provider)}</select>
</div>
<div class='field'>
    <span>Model</span>
    <input id='llmModelInput' class='control' type='text' value='${escapeHtml(draft.modelId)}' placeholder='gpt-4o' ${state.isSaving ? 'disabled' : ''}>
</div>
<div class='field'>
    <span>Endpoint</span>
    <input id='llmEndpointInput' class='control' type='url' value='${escapeHtml(draft.endpoint)}' placeholder='https://api.example.com/v1' ${state.isSaving ? 'disabled' : ''}>
    <div class='field-hint'>Use an absolute URL for the selected provider endpoint.</div>
</div>
<div class='settings-actions'>
    <button type='button' class='secondary-button' id='llmCancelButton' ${state.isSaving ? 'disabled' : ''}>Cancel</button>
    <button type='button' class='primary-button' id='llmSaveButton' ${state.isSaving ? 'disabled' : ''}>${state.isSaving ? 'Saving…' : 'Save settings'}</button>
</div>`;
        }

        return `
<div class='section-heading'>
    <div>
        <h2>Current configuration</h2>
        <p>Provider defaults are read from the active runtime configuration.</p>
    </div>
    <div class='settings-actions'>
        <button type='button' class='secondary-button' id='llmRefreshButton'>Refresh</button>
        <button type='button' class='primary-button' id='llmEditButton'>Edit</button>
    </div>
</div>
${banner}
<div class='definition-grid'>
    <div class='definition-item'>
        <div class='definition-label'>Provider</div>
        <div class='definition-value'>${escapeHtml(config.provider || 'unknown')}</div>
    </div>
    <div class='definition-item'>
        <div class='definition-label'>Model</div>
        <div class='definition-value'>${escapeHtml(config.modelId || 'unknown')}</div>
    </div>
    <div class='definition-item'>
        <div class='definition-label'>Endpoint</div>
        <div class='definition-value'>${escapeHtml(config.endpoint || 'Not configured')}</div>
    </div>
</div>
<p class='helper-text'>Only non-sensitive configuration values are displayed here. Save responses include a restart notice when runtime services need to be reloaded.</p>`;
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

        return `
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
</div>
<p class='helper-text'>The server currently returns the full summary. Query filters such as provider and date ranges are available to future UI refinements.</p>`;
    }

    function renderLlmSettingsPanels(){
        var configCard = document.getElementById('llmConfigCard');
        var usageCard = document.getElementById('llmUsageCard');
        if(!configCard || !usageCard){
            return;
        }

        configCard.innerHTML = renderLlmConfigCard();
        usageCard.innerHTML = renderLlmUsageCard();

        var refreshButton = document.getElementById('llmRefreshButton');
        if(refreshButton){
            refreshButton.onclick = function(){ loadLlmSettings(true); };
        }
        var usageRefreshButton = document.getElementById('llmUsageRefreshButton');
        if(usageRefreshButton){
            usageRefreshButton.onclick = function(){ loadLlmSettings(true); };
        }
        var editButton = document.getElementById('llmEditButton');
        if(editButton){
            editButton.onclick = function(){
                dashboardState.llm.draft = buildLlmDraft();
                dashboardState.llm.isEditing = true;
                dashboardState.llm.error = '';
                renderLlmSettingsPanels();
            };
        }
        var cancelButton = document.getElementById('llmCancelButton');
        if(cancelButton){
            cancelButton.onclick = function(){
                dashboardState.llm.isEditing = false;
                dashboardState.llm.draft = null;
                dashboardState.llm.error = '';
                renderLlmSettingsPanels();
            };
        }
        var saveButton = document.getElementById('llmSaveButton');
        if(saveButton){
            saveButton.onclick = saveLlmSettings;
        }

        var retryButtons = document.querySelectorAll('[data-llm-action="refresh"]');
        for(var i=0;i<retryButtons.length;i++){
            retryButtons[i].onclick = function(){ loadLlmSettings(true); };
        }
    }

    function saveLlmSettings(){
        var state = dashboardState.llm;
        if(state.isSaving){
            return;
        }

        var providerSelect = document.getElementById('llmProviderSelect');
        var modelInput = document.getElementById('llmModelInput');
        var endpointInput = document.getElementById('llmEndpointInput');
        if(!providerSelect || !modelInput || !endpointInput){
            return;
        }

        var payload = {
            provider:providerSelect.value,
            modelId:modelInput.value.trim(),
            endpoint:endpointInput.value.trim()
        };

        if(!payload.provider || !payload.modelId || !payload.endpoint){
            state.error = 'Provider, model, and endpoint are required.';
            renderLlmSettingsPanels();
            return;
        }

        state.isSaving = true;
        state.error = '';
        state.message = '';
        renderLlmSettingsPanels();

        apiRequest('/api/system/llm-config', {
            method:'PUT',
            body:JSON.stringify(payload)
        }).then(function(result){
            state.config = result && result.configuration ? result.configuration : payload;
            state.message = result && result.message
                ? result.message
                : 'LLM settings saved successfully.';
            state.isEditing = false;
            state.draft = null;
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isSaving = false;
            renderLlmSettingsPanels();
        });
    }

    // Agents page.
    function renderAgentsPage(){
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
    <div class='catalog-list' id='agentsList'>${renderLoadingPanel('Loading agents…')}</div>
</section>`;
    }

    function initAgentsPage(){
        renderAgentsList();
        if(!dashboardState.agents.items){
            loadAgents(false);
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
        }).catch(function(error){
            state.error = error.message;
        }).finally(function(){
            state.isLoading = false;
            renderAgentsList();
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
        renderAgentsList();
        if(state.expandedName === name && !state.detailByName[name]){
            loadAgentDetail(name);
        }
    }

    function renderAgentDetail(summary, detail, isLoading, error){
        if(isLoading){
            return `<div class='expand-panel'>${renderLoadingPanel('Loading agent details…')}</div>`;
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
        return `
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
    </div>
</div>`;
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
            container.innerHTML = html + renderLoadingPanel('Loading agents…');
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
            html += `
<article class='card catalog-card'>
    <button type='button' class='catalog-trigger' data-agent-name='${escapeHtml(summary.name)}' aria-expanded='${expanded ? 'true' : 'false'}'>
        <div class='catalog-summary'>
            <div>
                <h2 class='catalog-title'>${escapeHtml(summary.name)}</h2>
                <p class='catalog-description'>${escapeHtml(summary.description || 'No description provided.')}</p>
                <div class='badge-row'>
                    ${renderAutonomyBadge(summary.autonomyLevel)}
                    ${renderCountBadge(summary.skillCount, 'skill')}
                </div>
            </div>
            <span class='expand-icon' aria-hidden='true'>${expanded ? '−' : '+'}</span>
        </div>
    </button>
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

        var trigger = event.target.closest('[data-agent-name]');
        if(trigger){
            toggleAgentExpansion(trigger.getAttribute('data-agent-name'));
        }
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

    // User settings page.
    function renderUserSettingsPage(){
        queuePageInit(initUserSettingsPage);
        return `
<!-- Page: User Settings -->
<section class='page-shell'>
    <header class='page-header'>
        <div>
            <h1>User Settings</h1>
            <p>Maintain your profile, manage stored preferences, and add assistant-specific defaults that help OpenVEPA work autonomously.</p>
        </div>
        <span class='pill'>Personalization</span>
    </header>
    <div id='userSettingsStatus'></div>
    <div class='settings-grid'>
        <section class='card settings-card' id='profileSection'>${renderLoadingPanel('Loading profile…')}</section>
        <section class='card settings-card' id='assistantPreferencesSection'>${renderLoadingPanel('Loading assistant preferences…')}</section>
    </div>
    <section class='card settings-card' id='addPreferenceSection'>${renderLoadingPanel('Loading preference form…')}</section>
    <div class='section-stack' id='preferenceGroups'>${renderLoadingPanel('Loading preferences…')}</div>
</section>`;
    }

    function initUserSettingsPage(){
        renderUserSettingsSections();
        if(!dashboardState.preferences.groups){
            loadPreferences(false);
        }
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

    function getAssistantQuickAddDefinitions(){
        var timeZone = 'UTC';
        try{
            var resolved = Intl.DateTimeFormat().resolvedOptions();
            if(resolved && resolved.timeZone){
                timeZone = resolved.timeZone;
            }
        } catch(error){
        }

        return [
            { key:'assistant.riskTolerance', value:'balanced', category:'assistant', label:'Risk tolerance', detail:'balanced' },
            { key:'assistant.communicationStyle', value:'concise', category:'assistant', label:'Communication style', detail:'concise' },
            { key:'user.timezone', value:timeZone, category:'locale', label:'Timezone', detail:timeZone },
            { key:'user.preferredLanguage', value:navigator.language || 'en-US', category:'locale', label:'Preferred language', detail:navigator.language || 'en-US' }
        ];
    }

    function renderProfileSection(){
        var state = dashboardState.preferences;
        if(state.isLoading && !state.groups){
            return renderLoadingPanel('Loading profile…');
        }

        var nameEntry = findPreferenceEntry(state.groups, 'user.name');
        var emailEntry = findPreferenceEntry(state.groups, 'user.email');
        return `
<form id='profileForm'>
    <div class='section-heading'>
        <div>
            <h2>Profile</h2>
            <p>Stored as <span class='inline-code'>user.name</span> and <span class='inline-code'>user.email</span> preferences.</p>
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
    <p class='helper-text'>Leave a field blank to remove that stored value.</p>
</form>`;
    }

    function renderAssistantPreferencesSection(){
        var state = dashboardState.preferences;
        var quickAdds = getAssistantQuickAddDefinitions();
        var buttonsHtml = '';
        for(var i=0;i<quickAdds.length;i++){
            var item = quickAdds[i];
            buttonsHtml += `
<button type='button' class='secondary-button quick-add-button' data-quick-key='${escapeHtml(item.key)}' data-quick-value='${escapeHtml(item.value)}' data-quick-category='${escapeHtml(item.category)}' ${state.isMutating ? 'disabled' : ''}>
    <strong>${escapeHtml(item.label)}</strong>
    <span class='field-hint'>${escapeHtml(item.detail)}</span>
</button>`;
        }

        return `
<div class='section-heading'>
    <div>
        <h2>Assistant preferences</h2>
        <p>These help the assistant act autonomously with less clarification and better defaults.</p>
    </div>
    <span class='badge is-accent'>Quick add</span>
</div>
<p class='assistant-note'>Examples include risk tolerance, communication style, timezone, and preferred language. Use the buttons below to store common defaults instantly.</p>
<div class='quick-add-grid'>${buttonsHtml}
</div>`;
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

    function renderUserSettingsSections(){
        var state = dashboardState.preferences;
        var statusHost = document.getElementById('userSettingsStatus');
        var profileSection = document.getElementById('profileSection');
        var assistantSection = document.getElementById('assistantPreferencesSection');
        var addPreferenceSection = document.getElementById('addPreferenceSection');
        var preferenceGroups = document.getElementById('preferenceGroups');
        if(!statusHost || !profileSection || !assistantSection || !addPreferenceSection || !preferenceGroups){
            return;
        }

        statusHost.innerHTML = renderStatusBanner(state.error, 'error') || renderStatusBanner(state.message, 'success');
        profileSection.innerHTML = renderProfileSection();
        assistantSection.innerHTML = renderAssistantPreferencesSection();
        addPreferenceSection.innerHTML = renderAddPreferenceSection();
        preferenceGroups.innerHTML = renderPreferenceGroups();

        var profileForm = document.getElementById('profileForm');
        if(profileForm){
            profileForm.onsubmit = function(event){
                event.preventDefault();
                saveProfileSettings();
            };
        }

        var addPreferenceForm = document.getElementById('addPreferenceForm');
        if(addPreferenceForm){
            addPreferenceForm.onsubmit = function(event){
                event.preventDefault();
                saveNewPreference();
            };
        }

        assistantSection.onclick = handleAssistantPreferencesClick;
        preferenceGroups.onclick = handlePreferenceGroupsClick;

        var reloadButton = preferenceGroups.querySelector('[data-preferences-action="reload"]');
        if(reloadButton){
            reloadButton.onclick = function(){ loadPreferences(true); };
        }
    }

    function saveProfileSettings(){
        var state = dashboardState.preferences;
        var nameInput = document.getElementById('profileNameInput');
        var emailInput = document.getElementById('profileEmailInput');
        if(!nameInput || !emailInput){
            return;
        }

        var operations = [];
        var nameValue = nameInput.value.trim();
        var emailValue = emailInput.value.trim();
        var existingName = findPreferenceEntry(state.groups, 'user.name');
        var existingEmail = findPreferenceEntry(state.groups, 'user.email');

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

        if(operations.length === 0){
            state.error = '';
            state.message = 'Profile is already up to date.';
            renderUserSettingsSections();
            return;
        }

        runPreferenceMutation(function(){
            return Promise.all(operations);
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

    function handleAssistantPreferencesClick(event){
        var button = event.target.closest('[data-quick-key]');
        if(!button){
            return;
        }

        var key = button.getAttribute('data-quick-key');
        var value = button.getAttribute('data-quick-value');
        var category = button.getAttribute('data-quick-category');
        runPreferenceMutation(function(){
            return upsertPreference(key, value, category);
        }, 'Assistant preference saved.');
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

    sidebarToggle.addEventListener('click', toggleSidebar);
    sidebarOverlay.addEventListener('click', closeMobileSidebar);
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
    <p>${escapeHtml(sessionsPageState.errorMessage)}</p>
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
            feedback.textContent = sessionsPageState.errorMessage;
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

        if(response && response.status === 401){
            return 'Your session has expired. Sign in again and retry.';
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
        var token = localStorage.getItem(storageKeys.token);
        return token ? { 'Authorization': 'Bearer ' + token } : {};
    }

    renderNavigation();
    syncSidebarState();

    if(!routeMap[window.location.hash]){
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
