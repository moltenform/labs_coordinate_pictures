

def getFilesBadExtension(root, fnGetFiles, informat):
    return set(fnGetFiles(root)) - set(fnGetFiles(root, allowedexts=[informat]))

def cleanup(dir, recurse=False, informat='jpg'):
    run = getInputBool('run cleanup?')
    if not run: return
    print '---'
    fnGetFiles = files.recursefiles if recurse else files.listfiles
    for fullpath, name in fnGetFiles(dir, allowedexts=[informat]):
        percentage, sPathNew,type = parseFilepathGetPercentage(fullpath)
        if percentage == None:
            continue
        
        if files.exists(sPathNew) and '__MARKAS__' in name:
            print 'deleting '+name
            softDeleteFile(fullpath)
        else:
            print 'not deleting '+name+' because has not been processed yet'
            continue
