#!/usr/bin/env python3
"""Baseline-guarded Skill copy. Dry-run unless --apply. No Git/remote mutations."""
import argparse,hashlib,json,subprocess,sys
from pathlib import Path

def tree(root):
    return {str(p.relative_to(root)):p.read_bytes() for p in root.rglob('*') if p.is_file() and not any(x in p.relative_to(root).parts for x in ['.git','__pycache__']) and p.name!='.sync-source.json'}

def run(root,*args):
    p=subprocess.run(['git','-C',str(root),*args],capture_output=True)
    return p.stdout if p.returncode==0 else None

def digest(files):
    return hashlib.sha256(json.dumps({k:hashlib.sha256(v).hexdigest() for k,v in sorted(files.items())},sort_keys=True).encode()).hexdigest()

def main():
    p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True);p.add_argument('--target',type=Path,action='append',required=True);p.add_argument('--baseline-ref',default='HEAD');p.add_argument('--apply',action='store_true');a=p.parse_args();source=a.source.resolve();files=tree(source)
    if 'SKILL.md' not in files:raise SystemExit('Source SKILL.md missing')
    baseline_names=run(source,'ls-tree','-r','--name-only',a.baseline_ref)
    if baseline_names is None:raise SystemExit('Baseline ref unavailable')
    baseline={name:run(source,'show',a.baseline_ref+':'+name) for name in baseline_names.decode().splitlines()}
    conflicts=[];targets=[]
    for raw in a.target:
        target=raw.resolve()
        if raw.is_symlink() or target==source or source in target.parents or target in source.parents:raise SystemExit('Unsafe target '+str(raw))
        repo_bytes=run(target if target.exists() else target.parent if target.parent.exists() else target.parent.parent,'rev-parse','--show-toplevel')
        repo=Path(repo_bytes.decode().strip()) if repo_bytes else None
        if target.exists() and any(p.is_symlink() for p in target.rglob('*')):raise SystemExit('Symlink inside target '+str(target))
        local=tree(target) if target.exists() else {}
        if repo:
            prefix=str(target.relative_to(repo))+'/'
            tracked=(run(repo,'ls-tree','-r','--name-only','HEAD','--',prefix) or b'').decode().splitlines()
            for path in tracked:
                rel=path[len(prefix):]
                if rel not in local:local[rel]=run(repo,'show','HEAD:'+path)
        verified_previous=False
        marker=target/'.sync-source.json'
        if marker.is_file():
            try:
                recorded=json.loads(marker.read_text())
                verified_previous=(recorded.get('contentSha256')==digest(local) and recorded.get('sourceRemote')==(run(source,'remote','get-url','origin') or b'').decode().strip())
            except (ValueError,OSError):pass
        for name,value in local.items():
            if name=='.sync-source.json':continue
            if name not in files or (not verified_previous and value!=files[name] and value!=baseline.get(name)):
                conflicts.append({'target':str(target),'file':name})
        targets.append(target)
    if conflicts:
        print(json.dumps({'applied':False,'conflicts':conflicts},ensure_ascii=False,indent=2));return 2
    manifest={'sourceRemote':(run(source,'remote','get-url','origin') or b'').decode().strip(),'baselineCommit':(run(source,'rev-parse',a.baseline_ref) or b'').decode().strip(),'sourceState':'working-tree' if (run(source,'status','--porcelain') or b'').strip() else 'committed','sourceCommit':(run(source,'rev-parse','HEAD') or b'').decode().strip(),'contentSha256':digest(files)}
    if a.apply:
        for target in targets:
            for name,data in files.items():
                out=target/name;out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(data)
            (target/'.sync-source.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({'applied':a.apply,'filesPerTarget':len(files),'targets':[str(x) for x in targets],'manifest':manifest,'conflicts':[]},ensure_ascii=False,indent=2));return 0

if __name__=='__main__':sys.exit(main())
