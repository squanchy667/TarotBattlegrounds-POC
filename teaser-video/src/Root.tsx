import { Composition } from "remotion";
import { TarotTeaser } from "./TarotTeaser";

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Composition
        id="TarotTeaser"
        component={TarotTeaser}
        durationInFrames={300}
        fps={30}
        width={1920}
        height={1080}
      />
    </>
  );
};
