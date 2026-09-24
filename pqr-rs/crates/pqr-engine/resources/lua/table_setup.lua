 
        function PQR_SetupTables()
            PQR = {}
            for i=0,4 do
                PQR[i] = {}
                PQR[i].priorityTable = {}
		        PQR[i].priorityTable.index = {}
		        PQR[i].priorityTable.name = {}
		        PQR[i].priorityTable.spid = {}
		        PQR[i].priorityTable.actn = {}
		        PQR[i].priorityTable.test = {}
                PQR[i].priorityTable.recast = {}
                PQR[i].priorityTable.targetCast = {}
                PQR[i].priorityTable.delay = {}
                PQR[i].priorityTable.cancelChannel = {}
                PQR[i].priorityTable.requireCombat = true
                PQR[i].priorityTable.luaBefore = {}
                PQR[i].priorityTable.luaAfter = {}
		        PQR.interruptTable = {}
		        PQR.interruptTable.spell = {}
                PQR.losTable = {}
                PQR.losTable.name = {}
                PQR.losTable.time = {}
            end
        end

        function PQR_AddAbility(rotationNumber, index, name, spid, actn, testCode, recastDelay, targetCast, cancelChannel, luaBefore, luaAfter)
		    PQR[rotationNumber].priorityTable.index[index] = index
		    PQR[rotationNumber].priorityTable.name[index] = name
		    PQR[rotationNumber].priorityTable.spid[index] = spid
		    PQR[rotationNumber].priorityTable.actn[index] = actn
		    PQR[rotationNumber].priorityTable.test[index] = testCode
            PQR[rotationNumber].priorityTable.recast[index] = recastDelay
            PQR[rotationNumber].priorityTable.targetCast[index] = targetCast
            PQR[rotationNumber].priorityTable.delay[index] = rotationNumber
            PQR[rotationNumber].priorityTable.cancelChannel[index] = cancelChannel
            PQR[rotationNumber].priorityTable.luaBefore[index] = luaBefore
            PQR[rotationNumber].priorityTable.luaAfter[index] = luaAfter
        end

        function PQR_AddInterrupt(spellName)
            for i=0,1023 do
			    if PQR.interruptTable.spell[i] == nil then
			        PQR.interruptTable.spell[i] = spellName
                    return
			    end
            end
        end
