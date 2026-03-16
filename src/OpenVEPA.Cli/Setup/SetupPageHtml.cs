namespace OpenVEPA.Cli.Setup;

/// <summary>Contains the embedded HTML for the WebUI setup wizard.</summary>
internal static class SetupPageHtml
{
    /// <summary>The complete HTML page for the multi-step setup wizard.</summary>
    internal const string Content = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>OpenVEPA Setup</title>
<style>
*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
body{
    font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Oxygen,Ubuntu,sans-serif;
    background:#1a1a2e;color:#e0e0e0;min-height:100vh;
    display:flex;justify-content:center;align-items:center;padding:2rem;
}
.card{
    background:#16213e;border-radius:12px;padding:2.5rem;
    max-width:620px;width:100%;box-shadow:0 8px 32px rgba(0,0,0,.3);
}
.brand{text-align:center;margin-bottom:1.5rem}
.brand h1{color:#50fa7b;font-size:2.2rem;letter-spacing:.12em;font-weight:800}
.brand p{color:#888;margin-top:.4rem;font-size:.95rem}
.stepper{display:flex;justify-content:center;gap:0;margin-bottom:2rem;flex-wrap:wrap}
.step{display:flex;align-items:center;gap:.3rem;font-size:.78rem;color:#555;padding:.3rem .6rem}
.step.active{color:#50fa7b;font-weight:700}
.step.completed{color:#40d66b}
.step-arrow{color:#333;margin:0 .2rem}
.wizard-page{display:none}
.wizard-page.active{display:block}
.section{margin-bottom:1.5rem}
.section-title{
    color:#50fa7b;font-size:.8rem;text-transform:uppercase;
    letter-spacing:.1em;margin-bottom:.75rem;
    border-bottom:1px solid #2a2a4e;padding-bottom:.5rem;
}
.step-description{color:#888;font-size:.82rem;margin-top:.3rem}
label{display:block;margin-bottom:.4rem;font-size:.9rem;color:#ccc}
input[type="text"],input[type="password"],input[type="number"]{
    width:100%;padding:.6rem .8rem;background:#0f3460;
    border:1px solid #2a2a4e;border-radius:6px;color:#e0e0e0;
    font-size:.9rem;margin-bottom:1rem;transition:border-color .2s;
}
input[type="text"]:focus,input[type="password"]:focus,input[type="number"]:focus{
    outline:none;border-color:#50fa7b;
}
select{
    width:100%;padding:.6rem .8rem;background:#0f3460;
    border:1px solid #2a2a4e;border-radius:6px;color:#e0e0e0;
    font-size:.9rem;margin-bottom:1rem;transition:border-color .2s;
    appearance:auto;
}
select:focus{outline:none;border-color:#50fa7b}
.model-row{display:flex;gap:.5rem;align-items:flex-start;margin-bottom:1rem}
.model-row select{flex:1;margin-bottom:0}
.btn-fetch{
    padding:.6rem 1rem;background:#2a2a4e;color:#ccc;
    border:1px solid #3a3a5e;border-radius:6px;font-size:.82rem;
    white-space:nowrap;cursor:pointer;transition:background .2s;
}
.btn-fetch:hover:not(:disabled){background:#3a3a5e}
.btn-fetch:disabled{opacity:.5;cursor:not-allowed}
input[type="checkbox"]{accent-color:#50fa7b;width:1.1rem;height:1.1rem}
.checkbox-group{display:flex;align-items:center;gap:.5rem;margin-bottom:1rem}
.checkbox-group label{margin-bottom:0;cursor:pointer}
.channel-block{
    background:#0f3460;border:1px solid #2a2a4e;border-radius:8px;
    padding:1rem;margin-bottom:1rem;
}
.channel-block.disabled{opacity:.5}
.channel-block label.coming-soon{color:#666;font-style:italic}
.buttons{display:flex;gap:1rem;margin-top:2rem}
button{
    padding:.75rem 1.5rem;border:none;border-radius:6px;
    font-size:.95rem;cursor:pointer;transition:background .2s,opacity .2s;
}
.btn-primary{background:#50fa7b;color:#1a1a2e;font-weight:700;flex:1}
.btn-primary:hover:not(:disabled){background:#40d66b}
.btn-secondary{background:#2a2a4e;color:#ccc}
.btn-secondary:hover:not(:disabled){background:#3a3a5e}
button:disabled{opacity:.5;cursor:not-allowed}
.review-table{width:100%;border-collapse:collapse}
.review-table td{padding:.5rem .8rem;border-bottom:1px solid #2a2a4e;font-size:.9rem}
.review-table td:first-child{color:#888;width:40%}
.status{
    text-align:center;padding:1rem;border-radius:6px;
    margin-top:1.5rem;display:none;font-size:.95rem;
}
.status.success{background:rgba(80,250,123,.15);color:#50fa7b;display:block}
.status.error{background:rgba(255,85,85,.15);color:#ff5555;display:block}
.status.cancelled{background:rgba(255,184,108,.15);color:#ffb86c;display:block}
.hidden{display:none!important}
@media(max-width:600px){
    body{padding:1rem}
    .card{padding:1.5rem}
    .buttons{flex-direction:column}
    .model-row{flex-direction:column}
}
</style>
</head>
<body>
<div class="card">
    <div class="brand">
        <h1>OpenVEPA</h1>
        <p>Setup Wizard</p>
    </div>

    <div class="stepper" id="stepper">
        <span class="step active" data-step="1">1 Provider</span>
        <span class="step-arrow">&rarr;</span>
        <span class="step" data-step="2">2 Auth</span>
        <span class="step-arrow">&rarr;</span>
        <span class="step" data-step="3">3 Model</span>
        <span class="step-arrow">&rarr;</span>
        <span class="step" data-step="4">4 Channels</span>
        <span class="step-arrow">&rarr;</span>
        <span class="step" data-step="5">5 Review</span>
    </div>

    <form id="setupForm">
        <!-- Step 1: LLM Provider -->
        <div class="wizard-page active" id="page-1">
            <div class="section">
                <div class="section-title">LLM Provider</div>
                <label for="provider">Choose your LLM provider</label>
                <select id="provider"></select>
                <div class="step-description" id="providerDesc"></div>
            </div>
        </div>

        <!-- Step 2: Authentication -->
        <div class="wizard-page" id="page-2">
            <div class="section">
                <div class="section-title">Authentication</div>
                <div id="authApiKeySection">
                    <label for="apiKey">API Key</label>
                    <input type="password" id="apiKey" placeholder="sk-...">
                </div>
                <div id="authEndpointSection">
                    <label for="endpoint">Provider Endpoint</label>
                    <input type="text" id="endpoint">
                </div>
                <div class="step-description" id="authDesc"></div>
            </div>
        </div>

        <!-- Step 3: Model Selection -->
        <div class="wizard-page" id="page-3">
            <div class="section">
                <div class="section-title">Model Selection</div>
                <p class="step-description" style="margin-bottom:1rem">Select the language model that will power your assistant.</p>
                <label for="modelId">Model</label>
                <div class="model-row">
                    <select id="modelId"></select>
                    <button type="button" class="btn-fetch hidden" id="fetchModelsBtn">Fetch Models</button>
                </div>
                <input type="text" id="customModel" class="hidden" placeholder="Enter custom model name">
            </div>
        </div>

        <!-- Step 4: Messaging Channels -->
        <div class="wizard-page" id="page-4">
            <div class="section">
                <div class="section-title">Remote Access Channels</div>
                <p class="step-description" style="margin-bottom:1rem">Optionally connect messaging apps to control your assistant remotely.</p>

                <div class="channel-block">
                    <div class="checkbox-group">
                        <input type="checkbox" id="telegramEnabled">
                        <label for="telegramEnabled">Telegram</label>
                    </div>
                    <div id="telegramTokenSection" class="hidden">
                        <label for="telegramBotToken">Bot Token</label>
                        <input type="text" id="telegramBotToken" placeholder="Enter your Telegram Bot Token from @BotFather">
                    </div>
                </div>

                <div class="channel-block disabled">
                    <div class="checkbox-group">
                        <input type="checkbox" id="whatsAppEnabled" disabled>
                        <label for="whatsAppEnabled" class="coming-soon">WhatsApp (Coming Soon)</label>
                    </div>
                    <p class="step-description">WhatsApp integration coming soon. Will require a WhatsApp Business API account.</p>
                </div>
            </div>
        </div>

        <!-- Step 5: Review & Confirm -->
        <div class="wizard-page" id="page-5">
            <div class="section">
                <div class="section-title">Review &amp; Confirm</div>
                <table class="review-table" id="reviewTable">
                    <tbody></tbody>
                </table>
            </div>
        </div>

        <div class="buttons">
            <button type="button" class="btn-secondary hidden" id="backBtn">Back</button>
            <button type="button" class="btn-primary" id="nextBtn">Next</button>
            <button type="button" class="btn-secondary" id="cancelBtn">Cancel</button>
        </div>
    </form>
    <div class="status" id="status"></div>
</div>
<script>
(function(){
    'use strict';

    var providers = {
        ollama: {
            name:'Ollama (Local)', requiresKey:false,
            endpoint:'http://localhost:11434', defaultModel:'llama3.2',
            models:['llama3.2','llama3.1','llama3.3','codellama','mistral','mixtral','phi3','gemma2','qwen2.5','deepseek-r1'],
            canFetch:true, fetchType:'ollama',
            desc:'Run models locally on your own hardware \u2014 free and private.'
        },
        openai: {
            name:'OpenAI', requiresKey:true,
            endpoint:'https://api.openai.com/v1', defaultModel:'gpt-4o',
            models:['gpt-4o','gpt-4o-mini','gpt-4-turbo','gpt-4','gpt-3.5-turbo','o1','o1-mini','o3-mini'],
            canFetch:true, fetchType:'openai',
            desc:'GPT-4o and the full OpenAI model family.'
        },
        google: {
            name:'Google (Gemini)', requiresKey:true,
            endpoint:'https://generativelanguage.googleapis.com/v1beta', defaultModel:'gemini-2.0-flash',
            models:['gemini-2.0-flash','gemini-2.0-flash-lite','gemini-1.5-pro','gemini-1.5-flash'],
            canFetch:true, fetchType:'google',
            desc:'Google Gemini models with multimodal capabilities.'
        },
        anthropic: {
            name:'Anthropic (Claude)', requiresKey:true,
            endpoint:'https://api.anthropic.com/v1', defaultModel:'claude-sonnet-4-20250514',
            models:['claude-sonnet-4-20250514','claude-3-5-sonnet-20241022','claude-3-5-haiku-20241022','claude-3-opus-20240229'],
            canFetch:false,
            desc:'Claude models by Anthropic \u2014 strong reasoning and safety.'
        },
        mistral: {
            name:'Mistral AI', requiresKey:true,
            endpoint:'https://api.mistral.ai/v1', defaultModel:'mistral-large-latest',
            models:['mistral-large-latest','mistral-medium-latest','mistral-small-latest','open-mistral-nemo','codestral-latest'],
            canFetch:true, fetchType:'openai',
            desc:'European AI lab with efficient open-weight models.'
        },
        groq: {
            name:'Groq', requiresKey:true,
            endpoint:'https://api.groq.com/openai/v1', defaultModel:'llama-3.3-70b-versatile',
            models:['llama-3.3-70b-versatile','llama-3.1-8b-instant','mixtral-8x7b-32768','gemma2-9b-it'],
            canFetch:true, fetchType:'openai',
            desc:'Ultra-fast inference on custom LPU hardware.'
        },
        azure: {
            name:'Azure OpenAI', requiresKey:true,
            endpoint:'', defaultModel:'gpt-4o',
            models:['gpt-4o','gpt-4o-mini','gpt-4-turbo','gpt-4','gpt-35-turbo'],
            canFetch:false,
            desc:'OpenAI models hosted on your Azure deployment.'
        },
        cohere: {
            name:'Cohere', requiresKey:true,
            endpoint:'https://api.cohere.com/v2', defaultModel:'command-r-plus',
            models:['command-r-plus','command-r','command-light'],
            canFetch:false,
            desc:'Enterprise-focused models with RAG strengths.'
        },
        together: {
            name:'Together AI', requiresKey:true,
            endpoint:'https://api.together.xyz/v1',
            defaultModel:'meta-llama/Llama-3.3-70B-Instruct-Turbo',
            models:['meta-llama/Llama-3.3-70B-Instruct-Turbo','mistralai/Mixtral-8x22B-Instruct-v0.1','Qwen/Qwen2.5-72B-Instruct-Turbo'],
            canFetch:true, fetchType:'openai',
            desc:'Run open-source models in the cloud at scale.'
        },
        perplexity: {
            name:'Perplexity', requiresKey:true,
            endpoint:'https://api.perplexity.ai', defaultModel:'sonar-pro',
            models:['sonar-pro','sonar','sonar-reasoning-pro','sonar-reasoning'],
            canFetch:true, fetchType:'openai',
            desc:'Search-augmented models with built-in web access.'
        }
    };

    // Cloud providers whose endpoint is fixed (user never edits it)
    var fixedEndpointProviders = ['openai','google','anthropic','mistral','groq','cohere','together','perplexity'];

    var currentStep = 1;
    var totalSteps = 5;

    var providerSelect = document.getElementById('provider');
    var providerDesc = document.getElementById('providerDesc');
    var authApiKeySection = document.getElementById('authApiKeySection');
    var authEndpointSection = document.getElementById('authEndpointSection');
    var authDesc = document.getElementById('authDesc');
    var apiKeyInput = document.getElementById('apiKey');
    var endpointInput = document.getElementById('endpoint');
    var modelSelect = document.getElementById('modelId');
    var customModelInput = document.getElementById('customModel');
    var fetchBtn = document.getElementById('fetchModelsBtn');
    var telegramEnabled = document.getElementById('telegramEnabled');
    var telegramTokenSection = document.getElementById('telegramTokenSection');
    var telegramBotToken = document.getElementById('telegramBotToken');
    var reviewTable = document.getElementById('reviewTable').querySelector('tbody');
    var form = document.getElementById('setupForm');
    var backBtn = document.getElementById('backBtn');
    var nextBtn = document.getElementById('nextBtn');
    var cancelBtn = document.getElementById('cancelBtn');
    var statusEl = document.getElementById('status');

    // Populate provider dropdown
    var keys = Object.keys(providers);
    for(var i=0;i<keys.length;i++){
        var opt = document.createElement('option');
        opt.value = keys[i];
        opt.textContent = providers[keys[i]].name;
        providerSelect.appendChild(opt);
    }

    updateProviderDesc();
    providerSelect.addEventListener('change', updateProviderDesc);

    function updateProviderDesc(){
        var p = providers[providerSelect.value];
        providerDesc.textContent = p.desc || '';
    }

    // Telegram checkbox toggle
    telegramEnabled.addEventListener('change', function(){
        if(telegramEnabled.checked){
            telegramTokenSection.classList.remove('hidden');
        } else {
            telegramTokenSection.classList.add('hidden');
            telegramBotToken.value = '';
        }
    });

    // Model custom toggle
    modelSelect.addEventListener('change', function(){
        if(modelSelect.value === '__custom__'){
            customModelInput.classList.remove('hidden');
            customModelInput.focus();
        } else {
            customModelInput.classList.add('hidden');
        }
    });

    function populateModels(models, defaultModel){
        modelSelect.innerHTML = '';
        for(var i=0;i<models.length;i++){
            var opt = document.createElement('option');
            opt.value = models[i];
            opt.textContent = models[i];
            if(models[i] === defaultModel) opt.selected = true;
            modelSelect.appendChild(opt);
        }
        var custom = document.createElement('option');
        custom.value = '__custom__';
        custom.textContent = '\u2014 Custom model \u2014';
        modelSelect.appendChild(custom);
    }

    // Fetch models via backend proxy
    function fetchModels(showButton){
        var pk = providerSelect.value;
        var p = providers[pk];
        if(!p.canFetch) return;

        if(showButton){
            fetchBtn.textContent = 'Loading\u2026';
            fetchBtn.disabled = true;
        }

        var params = 'provider=' + encodeURIComponent(pk);
        if(apiKeyInput.value) params += '&apiKey=' + encodeURIComponent(apiKeyInput.value);
        var ep = endpointInput.value || p.endpoint;
        if(ep) params += '&endpoint=' + encodeURIComponent(ep);

        fetch('/init/api/models?' + params)
            .then(function(res){
                if(!res.ok) return res.json().then(function(d){ throw new Error(d.error || 'HTTP ' + res.status); });
                return res.json();
            })
            .then(function(data){
                if(data.models && data.models.length > 0){
                    populateModels(data.models, p.defaultModel);
                }
            })
            .catch(function(err){
                console.warn('Model fetch failed:', err.message);
            })
            .finally(function(){
                if(showButton){
                    fetchBtn.textContent = 'Fetch Models';
                    fetchBtn.disabled = false;
                }
            });
    }

    fetchBtn.addEventListener('click', function(){ fetchModels(true); });

    function showStatus(className, message){
        statusEl.className = 'status ' + className;
        statusEl.textContent = message;
    }

    // --- Wizard navigation ---

    function goToStep(step){
        if(step < 1 || step > totalSteps) return;
        currentStep = step;
        // Update pages
        var pages = document.querySelectorAll('.wizard-page');
        for(var i=0;i<pages.length;i++){
            pages[i].classList.remove('active');
        }
        document.getElementById('page-' + step).classList.add('active');
        // Update stepper
        var steps = document.querySelectorAll('.step');
        for(var i=0;i<steps.length;i++){
            var s = parseInt(steps[i].getAttribute('data-step'));
            steps[i].classList.remove('active','completed');
            if(s === step) steps[i].classList.add('active');
            else if(s < step) steps[i].classList.add('completed');
        }
        // Back button visibility
        if(step === 1) backBtn.classList.add('hidden');
        else backBtn.classList.remove('hidden');
        // Next button label
        if(step === 4) nextBtn.textContent = 'Review';
        else if(step === 5) nextBtn.textContent = 'Save Configuration';
        else nextBtn.textContent = 'Next';
        // Clear status on navigation
        statusEl.className = 'status';
        statusEl.textContent = '';
    }

    function onEnterStep(step){
        var pk = providerSelect.value;
        var p = providers[pk];
        if(step === 2){
            // Configure auth page based on provider
            var isOllama = pk === 'ollama';
            var isAzure = pk === 'azure';
            var isFixed = fixedEndpointProviders.indexOf(pk) !== -1;

            if(isOllama){
                authApiKeySection.classList.add('hidden');
                authEndpointSection.classList.remove('hidden');
                endpointInput.value = endpointInput.value || p.endpoint;
                authDesc.textContent = 'Ollama runs locally. Confirm or change the endpoint below.';
            } else if(isAzure){
                authApiKeySection.classList.remove('hidden');
                authEndpointSection.classList.remove('hidden');
                endpointInput.value = endpointInput.value || '';
                endpointInput.placeholder = 'https://your-resource.openai.azure.com/openai/deployments/your-deployment';
                authDesc.textContent = 'Enter your Azure OpenAI API key and deployment endpoint.';
            } else {
                // Public cloud: API key only, endpoint auto-filled
                authApiKeySection.classList.remove('hidden');
                authEndpointSection.classList.add('hidden');
                endpointInput.value = p.endpoint;
                authDesc.textContent = 'Enter your ' + p.name + ' API key to authenticate.';
            }
        }
        if(step === 3){
            // Populate models for selected provider (hardcoded fallback)
            populateModels(p.models, p.defaultModel);
            customModelInput.classList.add('hidden');
            customModelInput.value = '';
            if(p.canFetch){
                fetchBtn.classList.remove('hidden');
                fetchModels(false);
            } else {
                fetchBtn.classList.add('hidden');
            }
        }
        if(step === 5){
            buildReview();
        }
    }

    function buildReview(){
        var pk = providerSelect.value;
        var p = providers[pk];
        var modelId = modelSelect.value === '__custom__' ? customModelInput.value : modelSelect.value;
        var ep = endpointInput.value || p.endpoint;
        var hasKey = apiKeyInput.value ? 'Provided (\u2713)' : 'Not set';

        var rows = [
            ['Provider', p.name],
            ['Model', modelId || '(none)'],
            ['Endpoint', ep],
            ['API Key', p.requiresKey ? hasKey : 'Not required'],
            ['Telegram', telegramEnabled.checked ? 'Enabled' : 'Disabled'],
            ['WhatsApp', 'Disabled']
        ];

        reviewTable.innerHTML = '';
        for(var i=0;i<rows.length;i++){
            var tr = document.createElement('tr');
            var td1 = document.createElement('td');
            td1.textContent = rows[i][0];
            var td2 = document.createElement('td');
            td2.textContent = rows[i][1];
            tr.appendChild(td1);
            tr.appendChild(td2);
            reviewTable.appendChild(tr);
        }
    }

    function validateStep(step){
        var pk = providerSelect.value;
        var p = providers[pk];
        if(step === 2){
            if(p.requiresKey && !apiKeyInput.value){
                showStatus('error','Please enter your API key.');
                return false;
            }
            if(pk === 'azure' && !endpointInput.value){
                showStatus('error','Please enter your Azure deployment endpoint.');
                return false;
            }
        }
        if(step === 3){
            var modelId = modelSelect.value === '__custom__' ? customModelInput.value : modelSelect.value;
            if(!modelId){
                showStatus('error','Please select or enter a model.');
                return false;
            }
        }
        return true;
    }

    function submitConfig(){
        var pk = providerSelect.value;
        var p = providers[pk];
        var modelId = modelSelect.value === '__custom__' ? customModelInput.value : modelSelect.value;
        var ep = endpointInput.value || p.endpoint;

        var config = {
            provider: pk,
            apiKey: apiKeyInput.value || '',
            modelId: modelId,
            endpoint: ep,
            homePath: '~/.openvepa',
            port: 8371,
            schedulerEnabled: true,
            telegramBotToken: telegramEnabled.checked ? telegramBotToken.value : '',
            whatsAppEnabled: false
        };

        nextBtn.disabled = true;
        backBtn.disabled = true;
        cancelBtn.disabled = true;

        fetch('/init/api/setup', {
            method: 'POST',
            body: JSON.stringify(config),
            headers: {'Content-Type': 'application/json'}
        }).then(function(response){
            if(!response.ok) throw new Error('Server returned ' + response.status);
            showStatus('success', 'Configuration saved! Redirecting...');
            form.style.display = 'none';
            setTimeout(function(){ window.location.href = '/'; }, 1500);
        }).catch(function(err){
            showStatus('error', 'Failed to save: ' + err.message);
            nextBtn.disabled = false;
            backBtn.disabled = false;
            cancelBtn.disabled = false;
        });
    }

    nextBtn.addEventListener('click', function(){
        if(currentStep === totalSteps){
            submitConfig();
            return;
        }
        if(!validateStep(currentStep)) return;
        var next = currentStep + 1;
        onEnterStep(next);
        goToStep(next);
    });

    backBtn.addEventListener('click', function(){
        if(currentStep > 1){
            var prev = currentStep - 1;
            onEnterStep(prev);
            goToStep(prev);
        }
    });

    cancelBtn.addEventListener('click', function(){
        nextBtn.disabled = true;
        backBtn.disabled = true;
        cancelBtn.disabled = true;
        fetch('/init/api/cancel', {method: 'POST'}).catch(function(){});
        showStatus('cancelled', 'Setup cancelled.');
        form.style.display = 'none';
    });

    // Initialize step 1
    goToStep(1);
})();
</script>
</body>
</html>
""";
}