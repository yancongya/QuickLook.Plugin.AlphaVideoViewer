#!/usr/bin/env python3
"""Read-only repository alignment scan. Missing sparse files are read from HEAD."""
import argparse, datetime, hashlib, json, re, subprocess
from pathlib import Path

def git(root,*args):
    p=subprocess.run(['git','-C',str(root),*args],capture_output=True)
    return p.stdout if p.returncode==0 else None

def read(root,path):
    f=root/path
    return f.read_bytes() if f.is_file() else git(root,'show','HEAD:'+path)

def text(root,path):
    return (read(root,path) or b'').decode('utf-8-sig',errors='replace')

def audit(workspace,remote=None):
    result={'checkedAt':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':'Static working-tree/HEAD evidence; not Windows or live deployment acceptance','repositories':[]}
    for item in json.loads((workspace/'repos.json').read_text())['repos']:
        if item.get('kind')!='plugin':continue
        root=workspace/item['name'];plugin=item['pluginName'];site=text(root,'docs/index.html');readme=text(root,'README.md');build=text(root,'Scripts/build.ps1');workflow=text(root,'.github/workflows/build.yml');changelog=text(root,'CHANGELOG.md')
        found=[]
        def flag(code,detail):found.append({'code':code,'detail':detail})
        if not workflow:flag('workflow-missing','No working-tree or HEAD build workflow')
        if plugin+'.qlplugin' not in build:flag('package-name','Packaging does not contain expected package name')
        if not re.search(r'PLUGIN_NAME:\s*'+re.escape(plugin)+r'\s',workflow):flag('ci-name','Workflow PLUGIN_NAME differs')
        if 'Scripts/build.ps1 -SkipBuild -SkipVersion' not in workflow:flag('ci-package','Workflow does not call canonical package entry')
        if site and not git(root,'ls-files','docs/index.html'):flag('landing-local-only','Landing exists only in working tree')
        if site and 'github.io' not in readme and 'docs/index.html' not in readme:flag('readme-site','README lacks landing entry; state deployment status before adding public URL')
        if site:
            missing=[label for label,pattern in [('canonical',r'rel=["\x27]canonical'),('open-graph',r'og:'),('twitter',r'twitter:'),('structured-data',r'application/ld\+json')] if not re.search(pattern,site,re.I)]
            if missing:flag('seo-missing',', '.join(missing))
        entry=text(root,'Plugin.cs');project=text(root,item['csproj']);priority=re.search(r'Priority\s*=>\s*(-?\d+)',entry)
        record={'name':item['name'],'plugin':plugin,'head':(git(root,'rev-parse','HEAD') or b'').decode().strip(),'dirty':(git(root,'status','--short') or b'').decode().splitlines(),'runtime':'WebView2' if 'Microsoft.Web.WebView2' in project else 'native WPF / inspect project','priority':int(priority.group(1)) if priority else None,'changelogFirstVersion':next(iter(re.findall(r'^## \[(\d[^\]]*)\]',changelog,re.M)),None),'expectedAsset':plugin+'.qlplugin','hasWorkflow':bool(workflow),'findings':found}
        if remote and plugin in remote:
            remote_record=remote[plugin]
            release=remote_record.get('/releases/latest',{})
            if release.get('ok'):
                data=json.loads(release['data']);assets=[a['name'] if isinstance(a,dict) else a for a in data.get('assets',[])];record['release']=data
                wrong=[a for a in assets if a.endswith('.qlplugin') and a!=record['expectedAsset']]
                if wrong:flag('release-extra-asset','Other plugin package(s): '+', '.join(wrong))
                if record['expectedAsset'] not in assets:flag('release-asset-missing','Expected package is absent')
                if record['changelogFirstVersion']!=data.get('tag_name','').lstrip('v'):flag('changelog-release','First numbered changelog and latest release differ; verify tag history')
            pages=remote_record.get('/pages',{})
            if pages.get('ok'):
                data=json.loads(pages['data']);record['pages']=data
                if data.get('status')!='built':flag('pages-not-built','Pages reports '+str(data.get('status')))
            elif '404' in pages.get('error',''):flag('pages-not-configured','Pages API returned 404; not evidence of a live site')
            else:flag('pages-unverified','Remote request failed; retry rather than infer missing')
            repo=remote_record.get('repo',{})
            if repo.get('ok') and not json.loads(repo['data']).get('homepage'):flag('homepage-empty','Repository homepage is empty')
        result['repositories'].append(record)
    return result

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--workspace',type=Path,required=True);p.add_argument('--remote-snapshot',type=Path);p.add_argument('--output',type=Path);a=p.parse_args()
    data=audit(a.workspace,json.loads(a.remote_snapshot.read_text()) if a.remote_snapshot else None);out=json.dumps(data,ensure_ascii=False,indent=2)+'\n'
    if a.output:a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(out)
    else:print(out)
