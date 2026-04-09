import {
  getUsersSelfQuests,
  putUsersSelfQuestsByIdClaim,
  putUsersSelfQuestsByIdReroll,
} from '#api/sdk.gen'

import type { UserQuest } from '~/models/quest'

export const getUserQuests = async (): Promise<UserQuest[]> =>
  (await getUsersSelfQuests({})).data!

export const claimQuestReward = (questId: number, characterId: number) =>
  putUsersSelfQuestsByIdClaim({ path: { id: questId }, body: { characterId } })

export const rerollQuest = (questId: number) =>
  putUsersSelfQuestsByIdReroll({ path: { id: questId } })
