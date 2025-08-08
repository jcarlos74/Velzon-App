import { UserInfo } from "./user-info";

export class AccessToken
{
    authenticated: boolean = false;
    creationDate: string = "";
    expiration: string = "";
    accessToken: string = "";
    refreshToken: string = "";
    userInfo!: UserInfo;
}
