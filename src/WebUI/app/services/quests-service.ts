import type { UserQuestViewModel } from '#api/types.gen'

import {
  getUsersSelfQuests,
  putUsersSelfQuestsByIdClaim,
  putUsersSelfQuestsByIdReroll,
} from '#api/sdk.gen'

export type { UserQuestViewModel }

export const getUserQuests = async (): Promise<UserQuestViewModel[]> =>
  (await getUsersSelfQuests({})).data!

export const claimQuestReward = async (questId: number, characterId: number): Promise<UserQuestViewModel> =>
  (await putUsersSelfQuestsByIdClaim({ path: { id: questId }, body: { characterId } })).data!

export const rerollQuest = (questId: number) =>
  putUsersSelfQuestsByIdReroll({ path: { id: questId }, body: {} })
