export interface Event {
  id: number;
  name: string;
  description: string;
  date: string | Date; // En JSON, les dates arrivent souvent sous forme de chaînes de caractères
  sceneId: number;
}
